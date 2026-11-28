using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Trades;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Orders;

public class PositionService(
    TradingDbContext dbContext,
    IIndianBrokerageCalculator brokerageCalculator,
    IMarketDataService marketDataService,
    ITradingNotificationService notificationService,
    ILogger<PositionService> logger) : IPositionService
{
    public async Task<Position?> UpdatePositionOnExecutionAsync(TradeExecution execution, CancellationToken ct = default)
    {
        var account = await dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstOrDefaultAsync(a => a.Id == execution.TradingAccountId, ct);

        if (account == null)
        {
            logger.LogError("Trading account {AccountId} not found for execution {ExecutionId}", execution.TradingAccountId, execution.Id);
            return null;
        }

        var order = execution.TradingOrder ?? await dbContext.TradingOrders
            .FirstOrDefaultAsync(o => o.Id == execution.TradingOrderId, ct);

        var productType = order?.ProductType ?? ProductType.MIS;
        var exchange = execution.Exchange ?? "NSE";

        if (execution.Side == OrderSide.Buy)
        {
            var buyCharges = brokerageCalculator.CalculateSingleLegCharges(
                OrderSide.Buy, productType, execution.Price, execution.Quantity, exchange);

            // Deduct buy cost + charges from available funds
            var buyTotalCost = (execution.Price * execution.Quantity) + buyCharges.TotalCharges;
            account.AvailableFunds = Math.Max(0m, account.AvailableFunds - buyTotalCost);

            var openPosition = await dbContext.Positions
                .FirstOrDefaultAsync(p => p.TradingAccountId == execution.TradingAccountId &&
                                          p.TradingMode == account.TradingMode &&
                                          p.InstrumentId == execution.InstrumentId &&
                                          p.ProductType == productType &&
                                          p.IsOpen, ct);

            if (openPosition == null)
            {
                openPosition = new Position
                {
                    Id = Guid.NewGuid(),
                    UserId = execution.UserId,
                    TradingAccountId = execution.TradingAccountId,
                    TradingMode = account.TradingMode,
                    InstrumentId = execution.InstrumentId,
                    StrategyVersionId = order?.StrategyVersionId,
                    Side = OrderSide.Buy,
                    ProductType = productType,
                    Quantity = execution.Quantity,
                    AverageBuyPrice = execution.Price,
                    CurrentPrice = execution.Price,
                    StopLoss = order?.TriggerPrice,
                    TotalCharges = buyCharges.TotalCharges,
                    IsOpen = true,
                    OpenedAtUtc = DateTime.UtcNow
                };
                dbContext.Positions.Add(openPosition);
            }
            else
            {
                var newTotalQty = openPosition.Quantity + execution.Quantity;
                if (newTotalQty > 0)
                {
                    openPosition.AverageBuyPrice = ((openPosition.Quantity * openPosition.AverageBuyPrice) +
                                                    (execution.Quantity * execution.Price)) / newTotalQty;
                }
                openPosition.Quantity = newTotalQty;
                openPosition.CurrentPrice = execution.Price;
                openPosition.TotalCharges += buyCharges.TotalCharges;
            }

            await dbContext.SaveChangesAsync(ct);
            return openPosition;
        }
        else // OrderSide.Sell
        {
            var sellCharges = brokerageCalculator.CalculateSingleLegCharges(
                OrderSide.Sell, productType, execution.Price, execution.Quantity, exchange);

            // Add sell revenue minus charges to available funds
            var sellNetCredit = (execution.Price * execution.Quantity) - sellCharges.TotalCharges;
            account.AvailableFunds += sellNetCredit;

            var openPosition = await dbContext.Positions
                .FirstOrDefaultAsync(p => p.TradingAccountId == execution.TradingAccountId &&
                                          p.InstrumentId == execution.InstrumentId &&
                                          p.ProductType == productType &&
                                          p.IsOpen, ct);

            if (openPosition != null)
            {
                var qtyToClose = Math.Min(openPosition.Quantity, execution.Quantity);
                var entryPrice = openPosition.AverageBuyPrice;
                var exitPrice = execution.Price;
                var grossPnL = (exitPrice - entryPrice) * qtyToClose;
                var netPnL = grossPnL - sellCharges.TotalCharges;
                var returnPct = entryPrice > 0 ? (grossPnL / (entryPrice * qtyToClose)) * 100m : 0m;

                openPosition.RealizedGrossPnL += grossPnL;
                openPosition.RealizedNetPnL += netPnL;
                openPosition.TotalCharges += sellCharges.TotalCharges;
                openPosition.Quantity -= qtyToClose;
                openPosition.CurrentPrice = exitPrice;

                var duration = DateTime.UtcNow - openPosition.OpenedAtUtc;
                var durationStr = duration.TotalHours >= 1
                    ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
                    : $"{duration.Minutes}m {duration.Seconds}s";

                // Determine exit trigger
                string exitTrigger = "StrategySignal";
                if (openPosition.StopLoss.HasValue && exitPrice <= openPosition.StopLoss.Value)
                {
                    exitTrigger = "StopLoss";
                }
                else if (openPosition.Target.HasValue && exitPrice >= openPosition.Target.Value)
                {
                    exitTrigger = "Target";
                }
                else if (order?.StatusMessage?.Contains("Square", StringComparison.OrdinalIgnoreCase) == true)
                {
                    exitTrigger = "SquareOff";
                }
                else if (order?.StatusMessage?.Contains("Manual", StringComparison.OrdinalIgnoreCase) == true)
                {
                    exitTrigger = "Manual";
                }

                if (openPosition.Quantity <= 0)
                {
                    openPosition.Quantity = 0;
                    openPosition.IsOpen = false;
                    openPosition.ClosedAtUtc = DateTime.UtcNow;
                    openPosition.ExitReason = order?.StatusMessage ?? $"{exitTrigger} Execution";
                }

                var instrument = await dbContext.Instruments.FirstOrDefaultAsync(i => i.Id == execution.InstrumentId, ct);
                var sellToast = new SellExecutionNotificationDto(
                    TradeExecutionId: execution.Id,
                    OrderId: execution.TradingOrderId,
                    PositionId: openPosition.Id,
                    TradingAccountId: execution.TradingAccountId,
                    Symbol: instrument?.TradingSymbol ?? "UNKNOWN",
                    CompanyName: instrument?.CompanyName ?? "Unknown Company",
                    Quantity: qtyToClose,
                    EntryPrice: entryPrice,
                    ExitPrice: exitPrice,
                    TotalInvestment: entryPrice * qtyToClose,
                    TotalExitValue: exitPrice * qtyToClose,
                    GrossPnL: Math.Round(grossPnL, 2),
                    ReturnPercent: Math.Round(returnPct, 2),
                    Charges: sellCharges,
                    NetRealizedPnL: Math.Round(netPnL, 2),
                    HoldDuration: durationStr,
                    ExitTrigger: exitTrigger,
                    EntryTimeUtc: openPosition.OpenedAtUtc,
                    ExecutedAtUtc: DateTime.UtcNow
                );

                if (exitTrigger == "StopLoss")
                {
                    await notificationService.PublishStopLossHitAsync(sellToast, ct);
                }
                else if (exitTrigger == "Target")
                {
                    await notificationService.PublishTargetHitAsync(sellToast, ct);
                }
                else
                {
                    await notificationService.PublishSellExecutionAsync(sellToast, ct);
                }

                await dbContext.SaveChangesAsync(ct);
                return openPosition;
            }

            await dbContext.SaveChangesAsync(ct);
            return null;
        }
    }

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(Guid tradingAccountId, bool openOnly = true, CancellationToken ct = default)
    {
        var query = dbContext.Positions
            .Include(p => p.Instrument)
            .Where(p => p.TradingAccountId == tradingAccountId
                && p.TradingAccount.IsActive
                && p.TradingAccount.BrokerConnection.IsActive
                && p.TradingMode == p.TradingAccount.TradingMode);

        if (openOnly)
        {
            query = query.Where(p => p.IsOpen);
        }

        var positions = await query
            .OrderByDescending(p => p.OpenedAtUtc)
            .ToListAsync(ct);

        var result = new List<PositionDto>(positions.Count);
        foreach (var pos in positions)
        {
            decimal currentLtp = pos.CurrentPrice;
            if (pos.TradingMode != TradingMode.Live || currentLtp <= 0)
            {
                try
                {
                    var quote = await marketDataService.GetQuoteAsync(pos.Instrument.TradingSymbol, pos.Instrument.Exchange, ct);
                    if (quote != null && quote.LastPrice > 0)
                    {
                        currentLtp = quote.LastPrice;
                        pos.CurrentPrice = currentLtp;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to refresh quote for {Symbol}", pos.Instrument.TradingSymbol);
                }
            }

            var unrealized = pos.IsOpen && pos.Quantity > 0 ? (currentLtp - pos.AverageBuyPrice) * pos.Quantity : 0m;

            result.Add(new PositionDto(
                Id: pos.Id,
                TradingAccountId: pos.TradingAccountId,
                InstrumentId: pos.InstrumentId,
                Symbol: pos.Instrument.TradingSymbol,
                CompanyName: pos.Instrument.CompanyName,
                Side: pos.Side.ToString(),
                ProductType: pos.ProductType.ToString(),
                Quantity: pos.Quantity,
                AverageBuyPrice: pos.AverageBuyPrice,
                CurrentPrice: currentLtp,
                StopLoss: pos.StopLoss,
                Target: pos.Target,
                UnrealizedPnL: Math.Round(unrealized, 2),
                RealizedGrossPnL: Math.Round(pos.RealizedGrossPnL, 2),
                RealizedNetPnL: Math.Round(pos.RealizedNetPnL, 2),
                TotalCharges: Math.Round(pos.TotalCharges, 2),
                IsOpen: pos.IsOpen,
                OpenedAtUtc: pos.OpenedAtUtc,
                ClosedAtUtc: pos.ClosedAtUtc,
                ExitReason: pos.ExitReason
            ));
        }

        return result;
    }

    public async Task<PortfolioSummaryDto> GetPortfolioSummaryAsync(Guid tradingAccountId, CancellationToken ct = default)
    {
        var account = await dbContext.TradingAccounts
            .FirstOrDefaultAsync(a => a.Id == tradingAccountId, ct);

        var positions = await GetPositionsAsync(tradingAccountId, openOnly: true, ct);
        var allPositions = await dbContext.Positions
            .Where(p => p.TradingAccountId == tradingAccountId)
            .ToListAsync(ct);

        var totalCurrentVal = positions.Sum(p => p.Quantity * p.CurrentPrice);
        var totalInvestmentVal = positions.Sum(p => p.Quantity * p.AverageBuyPrice);
        var totalUnrealized = positions.Sum(p => p.UnrealizedPnL);
        var totalRealizedGross = allPositions.Sum(p => p.RealizedGrossPnL);
        var totalRealizedNet = allPositions.Sum(p => p.RealizedNetPnL);
        var totalChargesPaid = allPositions.Sum(p => p.TotalCharges);

        return new PortfolioSummaryDto(
            TradingAccountId: tradingAccountId,
            OpenPositionsCount: positions.Count,
            TotalCurrentValue: Math.Round(totalCurrentVal, 2),
            TotalInvestmentValue: Math.Round(totalInvestmentVal, 2),
            TotalUnrealizedPnL: Math.Round(totalUnrealized, 2),
            TotalRealizedGrossPnL: Math.Round(totalRealizedGross, 2),
            TotalRealizedNetPnL: Math.Round(totalRealizedNet, 2),
            TotalChargesPaid: Math.Round(totalChargesPaid, 2),
            AvailableFunds: account?.AvailableFunds ?? 0m,
            Positions: positions
        );
    }

    public async Task<PositionDto?> GetPositionByIdAsync(Guid positionId, CancellationToken ct = default)
    {
        var pos = await dbContext.Positions
            .Include(p => p.Instrument)
            .FirstOrDefaultAsync(p => p.Id == positionId, ct);

        if (pos == null) return null;

        decimal currentLtp = pos.CurrentPrice;
        if (pos.TradingMode != TradingMode.Live || currentLtp <= 0)
        {
            try
            {
                var quote = await marketDataService.GetQuoteAsync(pos.Instrument.TradingSymbol, pos.Instrument.Exchange, ct);
                if (quote != null && quote.LastPrice > 0)
                {
                    currentLtp = quote.LastPrice;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to refresh quote for {Symbol}", pos.Instrument.TradingSymbol);
            }
        }

        var unrealized = pos.IsOpen && pos.Quantity > 0 ? (currentLtp - pos.AverageBuyPrice) * pos.Quantity : 0m;

        return new PositionDto(
            Id: pos.Id,
            TradingAccountId: pos.TradingAccountId,
            InstrumentId: pos.InstrumentId,
            Symbol: pos.Instrument.TradingSymbol,
            CompanyName: pos.Instrument.CompanyName,
            Side: pos.Side.ToString(),
            ProductType: pos.ProductType.ToString(),
            Quantity: pos.Quantity,
            AverageBuyPrice: pos.AverageBuyPrice,
            CurrentPrice: currentLtp,
            StopLoss: pos.StopLoss,
            Target: pos.Target,
            UnrealizedPnL: Math.Round(unrealized, 2),
            RealizedGrossPnL: Math.Round(pos.RealizedGrossPnL, 2),
            RealizedNetPnL: Math.Round(pos.RealizedNetPnL, 2),
            TotalCharges: Math.Round(pos.TotalCharges, 2),
            IsOpen: pos.IsOpen,
            OpenedAtUtc: pos.OpenedAtUtc,
            ClosedAtUtc: pos.ClosedAtUtc,
            ExitReason: pos.ExitReason
        );
    }
}
