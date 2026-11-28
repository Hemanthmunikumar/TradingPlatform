using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Risk;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Approvals;

public class TradeApprovalService(
    TradingDbContext dbContext,
    IRiskValidationService riskService,
    IOrderService orderService,
    ITradingNotificationService notificationService,
    ILogger<TradeApprovalService> logger) : ITradeApprovalService
{
    public async Task<PendingTradeApprovalDto> CreateApprovalAsync(CreateTradeApprovalRequest request, CancellationToken ct = default)
    {
        var account = await dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.Id == request.TradingAccountId, ct);

        if (account == null)
        {
            throw new ArgumentException($"Trading account {request.TradingAccountId} not found.");
        }

        var duration = request.ApprovalDurationSeconds > 0
            ? request.ApprovalDurationSeconds
            : (account.Configuration?.ApprovalDurationSeconds ?? 60);

        var deadline = DateTime.UtcNow.AddSeconds(duration);

        var approval = new PendingTradeApproval
        {
            Id = Guid.NewGuid(),
            UserId = account.UserId,
            TradingAccountId = request.TradingAccountId,
            TradingMode = account.TradingMode,
            TradingSignalId = request.TradingSignalId,
            Symbol = request.Symbol,
            CompanyName = request.CompanyName,
            Side = OrderSide.Buy,
            Quantity = request.Quantity,
            IndicativePrice = request.IndicativePrice,
            StopLoss = request.StopLoss,
            Target = request.Target,
            RiskAmount = request.RiskAmount ?? ((request.StopLoss.HasValue && request.IndicativePrice > request.StopLoss.Value)
                ? (request.IndicativePrice - request.StopLoss.Value) * request.Quantity
                : null),
            Reason = request.Reason,
            ApprovalDeadlineUtc = deadline,
            Status = ApprovalStatus.Pending,
            IdempotencyKey = Guid.NewGuid()
        };

        dbContext.PendingTradeApprovals.Add(approval);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created PendingTradeApproval {ApprovalId} for {Quantity} {Symbol} @ {Price}. Deadline: {Deadline} ({Duration}s)",
            approval.Id, approval.Quantity, approval.Symbol, approval.IndicativePrice, approval.ApprovalDeadlineUtc, duration);

        var dto = MapToDto(approval);
        await notificationService.PublishBuyApprovalRequestedAsync(dto, ct);

        return dto;
    }

    public async Task<ApprovalDecisionResultDto> RejectApprovalAsync(
        Guid approvalId,
        string? reason = null,
        Guid? decidedByUserId = null,
        CancellationToken ct = default)
    {
        var approval = await dbContext.PendingTradeApprovals
            .FirstOrDefaultAsync(p => p.Id == approvalId, ct);

        if (approval == null)
        {
            return new ApprovalDecisionResultDto(false, "Trade approval record not found.", "NotFound", DateTime.UtcNow);
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            return new ApprovalDecisionResultDto(false, $"Approval has already been resolved as '{approval.Status}'.", approval.Status.ToString(), DateTime.UtcNow);
        }

        // Check if deadline already passed
        if (DateTime.UtcNow >= approval.ApprovalDeadlineUtc)
        {
            approval.Status = ApprovalStatus.Expired;
            approval.DecidedAtUtc = DateTime.UtcNow;
            approval.DecisionReason = "Rejection arrived after 2-second deadline passed.";
            await dbContext.SaveChangesAsync(ct);

            return new ApprovalDecisionResultDto(false, "Rejection window has expired.", "Expired", DateTime.UtcNow);
        }

        approval.Status = ApprovalStatus.Rejected;
        approval.DecidedAtUtc = DateTime.UtcNow;
        approval.DecidedByUserId = decidedByUserId ?? approval.UserId;
        approval.DecisionReason = reason ?? "User rejected within rejection window";

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("BUY Approval {ApprovalId} REJECTED by user within rejection window. Reason: {Reason}",
            approval.Id, approval.DecisionReason);

        await notificationService.PublishApprovalDecisionAsync(approval.Id, "Rejected", approval.DecisionReason, ct);

        return new ApprovalDecisionResultDto(true, "BUY order rejected successfully. No broker order was placed.", "Rejected", DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<PendingTradeApprovalDto>> GetPendingApprovalsAsync(Guid? tradingAccountId = null, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var query = dbContext.PendingTradeApprovals
            .Where(p => p.Status == ApprovalStatus.Pending
                && p.ApprovalDeadlineUtc > now
                && p.TradingAccount.IsActive
                && p.TradingAccount.BrokerConnection.IsActive
                && p.TradingMode == p.TradingAccount.TradingMode);

        if (tradingAccountId.HasValue)
        {
            query = query.Where(p => p.TradingAccountId == tradingAccountId.Value);
        }

        var approvals = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        return approvals.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PendingTradeApprovalDto>> GetApprovalHistoryAsync(Guid? tradingAccountId = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = dbContext.PendingTradeApprovals
            .Where(p => p.TradingAccount.IsActive
                && p.TradingAccount.BrokerConnection.IsActive
                && p.TradingMode == p.TradingAccount.TradingMode);

        if (tradingAccountId.HasValue)
        {
            query = query.Where(p => p.TradingAccountId == tradingAccountId.Value);
        }

        var approvals = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return approvals.Select(MapToDto).ToList();
    }

    public async Task<int> ProcessExpiredApprovalsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expiredApprovals = await dbContext.PendingTradeApprovals
            .Include(p => p.TradingAccount)
                .ThenInclude(a => a.Configuration)
            .Where(p => p.Status == ApprovalStatus.Pending && p.ApprovalDeadlineUtc <= now)
            .ToListAsync(ct);

        if (expiredApprovals.Count == 0) return 0;

        int processed = 0;
        foreach (var approval in expiredApprovals)
        {
            var account = approval.TradingAccount;
            var autoExecute = account?.Configuration?.AutoExecuteOnTimeout ?? false;

            if (!autoExecute)
            {
                approval.Status = ApprovalStatus.Expired;
                approval.DecidedAtUtc = DateTime.UtcNow;
                approval.DecisionReason = "Approval timed out without auto-execute enabled.";
                await notificationService.PublishApprovalDecisionAsync(approval.Id, "Expired", approval.DecisionReason, ct);
                processed++;
                continue;
            }

            // Auto-execute enabled: Pre-submission risk re-validation
            var instrument = await dbContext.Instruments
                .FirstOrDefaultAsync(i => i.TradingSymbol == approval.Symbol, ct);

            if (instrument == null)
            {
                approval.Status = ApprovalStatus.Rejected;
                approval.DecidedAtUtc = DateTime.UtcNow;
                approval.DecisionReason = $"Instrument {approval.Symbol} not found during auto-execution.";
                await notificationService.PublishApprovalDecisionAsync(approval.Id, "Rejected", approval.DecisionReason, ct);
                processed++;
                continue;
            }

            var riskRequest = new OrderRiskEvaluationRequest(
                TradingAccountId: approval.TradingAccountId,
                InstrumentId: instrument.Id,
                Symbol: approval.Symbol,
                Side: "Buy",
                OrderType: "Market",
                ProductType: "MIS",
                Quantity: approval.Quantity,
                Price: approval.IndicativePrice,
                StopLoss: approval.StopLoss,
                Target: approval.Target
            );

            var riskResult = await riskService.ValidateOrderAsync(riskRequest, ct);
            if (!riskResult.IsApproved)
            {
                approval.Status = ApprovalStatus.Rejected;
                approval.DecidedAtUtc = DateTime.UtcNow;
                approval.DecisionReason = $"Risk gate re-validation failed: {string.Join("; ", riskResult.Failures.Select(f => f.Message))}";
                await notificationService.PublishApprovalDecisionAsync(approval.Id, "Rejected", approval.DecisionReason, ct);
                processed++;
                continue;
            }

            // Risk check passed -> Approve and execute order
            approval.Status = ApprovalStatus.Approved;
            approval.DecidedAtUtc = DateTime.UtcNow;
            approval.DecisionReason = "Auto-executed upon 2-second timeout completion";

            var orderRequest = new CreateOrderRequest(
                TradingAccountId: approval.TradingAccountId,
                InstrumentId: instrument.Id,
                Symbol: approval.Symbol,
                Side: "Buy",
                OrderType: "Market",
                ProductType: "MIS",
                Quantity: approval.Quantity,
                Price: approval.IndicativePrice,
                StopLoss: approval.StopLoss,
                Target: approval.Target,
                PendingTradeApprovalId: approval.Id
            );

            try
            {
                var order = await orderService.CreateOrderAsync(orderRequest, ct);
                var submitted = await orderService.SubmitOrderAsync(order.Id, ct);
                await notificationService.PublishApprovalDecisionAsync(approval.Id, "Approved", $"Order executed: {submitted.BrokerOrderId ?? submitted.Id.ToString()}", ct);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-execute order for approval {ApprovalId}", approval.Id);
                approval.Status = ApprovalStatus.Rejected;
                approval.DecisionReason = $"Execution failed: {ex.Message}";
                await notificationService.PublishApprovalDecisionAsync(approval.Id, "Rejected", approval.DecisionReason, ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return processed;
    }

    private static PendingTradeApprovalDto MapToDto(PendingTradeApproval a)
    {
        var now = DateTime.UtcNow;
        var remaining = Math.Max(0, (a.ApprovalDeadlineUtc - now).TotalSeconds);
        var isExpired = a.Status == ApprovalStatus.Pending && now >= a.ApprovalDeadlineUtc;

        return new PendingTradeApprovalDto(
            Id: a.Id,
            TradingAccountId: a.TradingAccountId,
            TradingSignalId: a.TradingSignalId,
            Symbol: a.Symbol,
            CompanyName: a.CompanyName,
            Side: a.Side.ToString(),
            Quantity: a.Quantity,
            IndicativePrice: a.IndicativePrice,
            EstimatedOrderValue: a.EstimatedOrderValue,
            StopLoss: a.StopLoss,
            Target: a.Target,
            RiskAmount: a.RiskAmount,
            Reason: a.Reason,
            ApprovalDeadlineUtc: a.ApprovalDeadlineUtc,
            Status: a.Status.ToString(),
            DecidedAtUtc: a.DecidedAtUtc,
            DecisionReason: a.DecisionReason,
            RemainingSeconds: Math.Round(remaining, 2),
            IsExpired: isExpired,
            CreatedAtUtc: a.CreatedAtUtc
        );
    }
}
