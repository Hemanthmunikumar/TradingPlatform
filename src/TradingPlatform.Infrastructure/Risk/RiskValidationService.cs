using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Contracts.Risk;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Risk;

public class RiskValidationService(
    ITradingDbContext dbContext,
    IMarketDataService marketDataService,
    ILogger<RiskValidationService> logger) : IRiskValidationService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeZoneInfo IstTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    public async Task<RiskEvaluationResultDto> ValidateOrderAsync(OrderRiskEvaluationRequest request, CancellationToken ct = default)
    {
        var failures = new List<RiskValidationFailureDto>();
        var warnings = new List<string>();

        // 1. Fetch Account and its configuration & broker connection
        var account = await dbContext.TradingAccounts
            .Include(a => a.BrokerConnection)
                .ThenInclude(c => c!.BrokerProvider)
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.Id == request.TradingAccountId, ct);

        if (account == null)
        {
            failures.Add(new RiskValidationFailureDto("RISK_000_ACCOUNT_NOT_FOUND", "Trading account does not exist.", "Error"));
            return new RiskEvaluationResultDto(false, failures, warnings, DateTime.UtcNow);
        }

        var config = account.Configuration ?? new AccountConfiguration
        {
            MaxCapitalAllocation = 500000,
            MaxOrderValue = 100000,
            MaxPositions = 5,
            MaxRiskPerTradePercent = 2.0m,
            DailyLossLimit = 10000,
            MaxExposurePerStockPercent = 25.0m,
            MaxTradesPerDay = 20,
            StopLossRequired = true,
            ApprovalDurationSeconds = 2,
            AutoExecuteOnTimeout = true,
            KillSwitchActive = false
        };

        // 2. System Level Kill Switch Check
        var systemKillSwitch = await dbContext.SystemConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "SystemKillSwitchActive", ct);

        if (systemKillSwitch != null && bool.TryParse(systemKillSwitch.Value, out var sysActive) && sysActive)
        {
            failures.Add(new RiskValidationFailureDto("RISK_001_SYSTEM_KILL_SWITCH", "System-wide emergency trading halt is engaged. All new order placement is blocked.", "Error"));
        }

        // 3. Account Level Kill Switch Check
        if (config.KillSwitchActive)
        {
            failures.Add(new RiskValidationFailureDto("RISK_002_ACCOUNT_KILL_SWITCH", $"Emergency kill switch is engaged for account '{account.AccountName}'.", "Error"));
        }

        // 4. Account Active & Trading Enabled
        if (!account.IsActive || !account.TradingEnabled)
        {
            failures.Add(new RiskValidationFailureDto("RISK_003_ACCOUNT_INACTIVE", $"Account '{account.AccountName}' is inactive or trading is disabled.", "Error"));
        }

        // 5. Broker Connection Authenticated
        if (account.BrokerConnection != null && !account.BrokerConnection.IsAuthenticated && account.BrokerConnection.BrokerProvider?.Code != BrokerCode.Mock)
        {
            failures.Add(new RiskValidationFailureDto("RISK_004_BROKER_AUTH", "Broker connection is not authenticated. Please re-authenticate via OAuth.", "Error"));
        }

        // 6. Market Hours Check
        var marketStatus = marketDataService.GetMarketStatus("NSE");
        if (!marketStatus.IsMarketOpen && account.TradingMode == TradingMode.Live)
        {
            failures.Add(new RiskValidationFailureDto("RISK_005_MARKET_HOURS", $"Indian equity market is currently {marketStatus.CurrentSession}. Session opens at 09:15 IST.", "Error"));
        }

        // 7. Instrument Active Check
        var instrument = await dbContext.Instruments.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InstrumentId, ct);
        if (instrument == null || !instrument.IsActive)
        {
            failures.Add(new RiskValidationFailureDto("RISK_006_INSTRUMENT_INACTIVE", $"Instrument '{request.Symbol}' not found or trading is suspended.", "Error"));
        }

        // 8. Schedule Bounds (IST)
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTimeZone);
        var currentTime = TimeOnly.FromTimeSpan(istNow.TimeOfDay);

        if (currentTime < config.TradingScheduleStart)
        {
            failures.Add(new RiskValidationFailureDto("RISK_007_SCHEDULE_START", $"Current time ({currentTime:HH\\:mm}) is before configured trading start ({config.TradingScheduleStart:HH\\:mm}).", "Error"));
        }

        if (currentTime > config.TradingScheduleEnd)
        {
            failures.Add(new RiskValidationFailureDto("RISK_007_SCHEDULE_END", $"Current time ({currentTime:HH\\:mm}) is after configured trading end ({config.TradingScheduleEnd:HH\\:mm}).", "Error"));
        }

        // 9. Intraday (MIS) Square-off Cutoff
        if (request.ProductType.Equals("MIS", StringComparison.OrdinalIgnoreCase))
        {
            if (currentTime >= config.IntradaySquareOffTime)
            {
                failures.Add(new RiskValidationFailureDto("RISK_008_INTRADAY_CUTOFF", $"Intraday MIS orders cannot be initiated after square-off time ({config.IntradaySquareOffTime:HH\\:mm}).", "Error"));
            }
        }

        // 10. Daily Loss Limit Check
        var dailySummary = await GetDailyRiskSummaryAsync(account.Id, ct);
        if (dailySummary.TotalNetPnL < 0 && Math.Abs(dailySummary.TotalNetPnL) >= config.DailyLossLimit)
        {
            // Auto-engage account kill switch
            if (!config.KillSwitchActive)
            {
                config.KillSwitchActive = true;
                await dbContext.SaveChangesAsync(ct);
                logger.LogWarning("Account {AccountName} exceeded daily loss limit ₹{Limit}. Auto-engaged kill switch.", account.AccountName, config.DailyLossLimit);
            }

            failures.Add(new RiskValidationFailureDto(
                "RISK_009_DAILY_LOSS_BREACH", 
                $"Daily loss limit (₹{config.DailyLossLimit:N2}) breached. Today's loss: ₹{Math.Abs(dailySummary.TotalNetPnL):N2}. Emergency Kill Switch auto-engaged.", 
                "Error"
            ));
        }

        // 11. Daily Trades Limit
        if (dailySummary.TradesCountToday >= config.MaxTradesPerDay)
        {
            failures.Add(new RiskValidationFailureDto("RISK_010_MAX_TRADES_EXCEEDED", $"Daily trade count limit ({config.MaxTradesPerDay}) reached for today.", "Error"));
        }

        // 12. Max Open Positions Count
        bool isBuy = request.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase);
        if (isBuy && dailySummary.OpenPositionsCount >= config.MaxPositions)
        {
            failures.Add(new RiskValidationFailureDto("RISK_011_MAX_POSITIONS_REACHED", $"Maximum open positions limit ({config.MaxPositions}) reached.", "Error"));
        }

        // 13. Max Single Order Value
        decimal orderValue = request.Quantity * request.Price;
        if (orderValue > config.MaxOrderValue)
        {
            failures.Add(new RiskValidationFailureDto("RISK_012_ORDER_VALUE_LIMIT", $"Order value ₹{orderValue:N2} exceeds maximum allowed single order value ₹{config.MaxOrderValue:N2}.", "Error"));
        }

        // 14. Max Capital Allocation
        if (isBuy && (orderValue > config.MaxCapitalAllocation))
        {
            failures.Add(new RiskValidationFailureDto("RISK_013_CAPITAL_ALLOCATION_LIMIT", $"Order value ₹{orderValue:N2} exceeds total allocated capital ₹{config.MaxCapitalAllocation:N2}.", "Error"));
        }

        // 15. Sufficient Available Funds / Margin
        if (isBuy && orderValue > account.AvailableFunds)
        {
            failures.Add(new RiskValidationFailureDto("RISK_014_INSUFFICIENT_FUNDS", $"Insufficient available funds (₹{account.AvailableFunds:N2}) for order value ₹{orderValue:N2}.", "Error"));
        }

        // 16. Per-Trade Risk % Limit
        if (isBuy && request.StopLoss.HasValue && request.StopLoss.Value < request.Price)
        {
            decimal riskAmount = (request.Price - request.StopLoss.Value) * request.Quantity;
            decimal capitalBase = account.AllocatedCapital > 0 ? account.AllocatedCapital : account.AvailableFunds;
            if (capitalBase > 0)
            {
                decimal riskPercent = (riskAmount / capitalBase) * 100m;
                if (riskPercent > config.MaxRiskPerTradePercent)
                {
                    failures.Add(new RiskValidationFailureDto("RISK_015_TRADE_RISK_LIMIT", $"Per-trade risk ({riskPercent:F2}%) exceeds maximum allowed risk ({config.MaxRiskPerTradePercent:F2}%).", "Error"));
                }
            }
        }

        // 17. Max Single Stock Exposure %
        if (isBuy)
        {
            var existingPositions = await dbContext.Positions
                .AsNoTracking()
                .Where(p => p.TradingAccountId == account.Id && p.InstrumentId == request.InstrumentId && p.Quantity > 0)
                .SumAsync(p => p.Quantity * p.AverageBuyPrice, ct);

            decimal totalStockExposure = existingPositions + orderValue;
            decimal totalCapital = account.AllocatedCapital > 0 ? account.AllocatedCapital : 500000m;
            decimal exposurePercent = (totalStockExposure / totalCapital) * 100m;

            if (exposurePercent > config.MaxExposurePerStockPercent)
            {
                failures.Add(new RiskValidationFailureDto("RISK_016_STOCK_EXPOSURE_LIMIT", $"Exposure to {request.Symbol} would reach {exposurePercent:F1}%, exceeding limit {config.MaxExposurePerStockPercent:F1}%.", "Error"));
            }
        }

        // 18. Mandatory Stop Loss
        if (isBuy && config.StopLossRequired)
        {
            if (!request.StopLoss.HasValue || request.StopLoss.Value <= 0 || request.StopLoss.Value >= request.Price)
            {
                failures.Add(new RiskValidationFailureDto("RISK_017_STOPLOSS_REQUIRED", "A valid stop-loss below the entry price is strictly mandatory for BUY orders.", "Error"));
            }
        }

        // 19. Duplicate Order Detection (same symbol, side, account within 5s window)
        var duplicateCutoff = DateTime.UtcNow.Subtract(DuplicateWindow);
        var recentDuplicate = await dbContext.TradingOrders
            .AsNoTracking()
            .Where(o => o.TradingAccountId == account.Id &&
                        o.InstrumentId == request.InstrumentId &&
                        o.CreatedAtUtc >= duplicateCutoff)
            .AnyAsync(ct);

        if (recentDuplicate)
        {
            failures.Add(new RiskValidationFailureDto("RISK_018_DUPLICATE_ORDER", $"Duplicate order detected for {request.Symbol} within the last {DuplicateWindow.TotalSeconds} seconds.", "Error"));
        }

        // 20. Price Reasonableness Check (deviation from current LTP > 5%)
        try
        {
            var quote = await marketDataService.GetQuoteAsync(request.Symbol, instrument?.Exchange ?? "NSE", ct);
            if (quote.LastPrice > 0)
            {
                decimal priceDeviation = Math.Abs(request.Price - quote.LastPrice) / quote.LastPrice;
                if (priceDeviation > 0.05m)
                {
                    failures.Add(new RiskValidationFailureDto("RISK_019_PRICE_DEVIATION", $"Order price ₹{request.Price:N2} deviates by {priceDeviation * 100:F1}% from current LTP ₹{quote.LastPrice:N2} (> 5% threshold).", "Error"));
                }
            }
        }
        catch { }

        bool isApproved = failures.Count == 0;

        return new RiskEvaluationResultDto(
            IsApproved: isApproved,
            Failures: failures,
            Warnings: warnings,
            EvaluatedAtUtc: DateTime.UtcNow
        );
    }

    public async Task<DailyRiskSummaryDto> GetDailyRiskSummaryAsync(Guid tradingAccountId, CancellationToken ct = default)
    {
        var account = await dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.Id == tradingAccountId, ct);

        var todayUtcStart = DateTime.UtcNow.Date;

        // Count trades today
        var tradesCount = await dbContext.TradeExecutions
            .AsNoTracking()
            .Where(t => t.TradingAccountId == tradingAccountId && t.ExecutedAtUtc >= todayUtcStart)
            .CountAsync(ct);

        // Count open positions
        var openPositions = await dbContext.Positions
            .AsNoTracking()
            .Where(p => p.TradingAccountId == tradingAccountId && p.Quantity != 0)
            .ToListAsync(ct);

        decimal realizedPnL = await dbContext.Positions
            .AsNoTracking()
            .Where(p => p.TradingAccountId == tradingAccountId)
            .SumAsync(p => p.RealizedNetPnL, ct);

        decimal unrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL);
        decimal netPnL = realizedPnL + unrealizedPnL;

        decimal limit = account?.Configuration?.DailyLossLimit ?? 10000m;
        decimal currentLoss = netPnL < 0 ? Math.Abs(netPnL) : 0m;
        decimal remainingBuffer = Math.Max(0, limit - currentLoss);

        return new DailyRiskSummaryDto(
            TradingAccountId: tradingAccountId,
            AccountName: account?.AccountName ?? "Trading Account",
            DailyLossLimit: limit,
            TodayRealizedPnL: realizedPnL,
            TodayUnrealizedPnL: unrealizedPnL,
            TotalNetPnL: netPnL,
            RemainingLossBuffer: remainingBuffer,
            TradesCountToday: tradesCount,
            MaxTradesPerDay: account?.Configuration?.MaxTradesPerDay ?? 20,
            OpenPositionsCount: openPositions.Count,
            MaxPositions: account?.Configuration?.MaxPositions ?? 5,
            KillSwitchActive: account?.Configuration?.KillSwitchActive ?? false
        );
    }

    public async Task<bool> ToggleKillSwitchAsync(ToggleKillSwitchRequest request, CancellationToken ct = default)
    {
        if (request.TradingAccountId.HasValue)
        {
            var config = await dbContext.AccountConfigurations
                .FirstOrDefaultAsync(c => c.TradingAccountId == request.TradingAccountId.Value, ct);

            if (config != null)
            {
                config.KillSwitchActive = request.Activate;
                await dbContext.SaveChangesAsync(ct);
                logger.LogWarning("Account Kill Switch set to {State} for account {AccountId}. Reason: {Reason}", request.Activate, request.TradingAccountId, request.Reason);
                return true;
            }
        }
        else
        {
            // Global System Kill Switch
            var sysConfig = await dbContext.SystemConfigurations
                .FirstOrDefaultAsync(s => s.Key == "SystemKillSwitchActive", ct);

            if (sysConfig == null)
            {
                sysConfig = new SystemConfiguration
                {
                    Key = "SystemKillSwitchActive",
                    Value = request.Activate.ToString().ToLowerInvariant(),
                    Description = "System-wide emergency halt switch"
                };
                dbContext.SystemConfigurations.Add(sysConfig);
            }
            else
            {
                sysConfig.Value = request.Activate.ToString().ToLowerInvariant();
            }

            await dbContext.SaveChangesAsync(ct);
            logger.LogCritical("GLOBAL SYSTEM KILL SWITCH set to {State}. Reason: {Reason}", request.Activate, request.Reason);
            return true;
        }

        return false;
    }
}
