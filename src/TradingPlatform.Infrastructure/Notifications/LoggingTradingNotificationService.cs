using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Infrastructure.Notifications;

public class LoggingTradingNotificationService(ILogger<LoggingTradingNotificationService> logger) : ITradingNotificationService
{
    public Task PublishBuyApprovalRequestedAsync(PendingTradeApprovalDto approval, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: BUY Approval Requested for {Symbol} (Qty: {Qty}, Price: {Price}). Expires in {Sec}s. ID: {Id}",
            approval.Symbol, approval.Quantity, approval.IndicativePrice, approval.RemainingSeconds, approval.Id);
        return Task.CompletedTask;
    }

    public Task PublishApprovalDecisionAsync(Guid approvalId, string status, string message, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Approval {Id} decision: {Status}. {Message}", approvalId, status, message);
        return Task.CompletedTask;
    }

    public Task PublishOrderSubmittedAsync(OrderDto order, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Order Submitted {OrderId} ({Side} {Qty} {Symbol})", order.Id, order.Side, order.Quantity, order.Symbol);
        return Task.CompletedTask;
    }

    public Task PublishOrderFilledAsync(OrderDto order, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Order Filled {OrderId} ({Side} {Qty} {Symbol} @ {AvgPrice})", order.Id, order.Side, order.Quantity, order.Symbol, order.AveragePrice);
        return Task.CompletedTask;
    }

    public Task PublishPositionClosedAsync(PositionDto position, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Position Closed {Symbol} RealizedNetPnL: {PnL:F2}, TotalCharges: {Charges:F2}",
            position.Symbol, position.RealizedNetPnL, position.TotalCharges);
        return Task.CompletedTask;
    }
}
