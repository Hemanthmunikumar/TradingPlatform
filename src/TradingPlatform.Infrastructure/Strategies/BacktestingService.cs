using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Application.Interfaces.Strategies;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Strategies;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Strategies;

public class BacktestingService(
    TradingDbContext dbContext,
    IMarketDataService marketDataService,
    IIndianBrokerageCalculator chargesCalculator,
    ILogger<BacktestingService> logger) : IBacktestingService
{
    public async Task<BacktestResultDto> RunBacktestAsync(BacktestRequestDto request, CancellationToken ct = default)
    {
        logger.LogInformation("Starting backtest for Strategy {StrategyId} on {Symbol} from {From} to {To}",
            request.StrategyDefinitionId, request.Symbol, request.FromUtc, request.ToUtc);

        var instrument = await dbContext.Instruments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TradingSymbol == request.Symbol && i.Exchange == request.Exchange, ct);

        IReadOnlyList<MarketCandleDto> candles = [];
        if (instrument != null)
        {
            var candleRequest = new GetCandlesRequest(
                InstrumentId: instrument.Id,
                Timeframe: request.Timeframe,
                FromUtc: request.FromUtc,
                ToUtc: request.ToUtc
            );
            candles = await marketDataService.GetHistoricalCandlesAsync(candleRequest, ct);
        }

        if (candles.Count < 20)
        {
            return new BacktestResultDto(
                request.StrategyDefinitionId, request.Symbol, request.FromUtc, request.ToUtc,
                request.InitialCapital, request.InitialCapital, 0m, 0m, 0, 0, 0, 0m, 0m, 0m, []
            );
        }

        var trades = new List<BacktestTradeDto>();
        decimal currentCapital = request.InitialCapital;
        decimal peakCapital = currentCapital;
        decimal maxDrawdown = 0m;

        bool inPosition = false;
        decimal entryPrice = 0m;
        DateTime entryTime = DateTime.MinValue;
        int qty = 0;

        for (int i = 20; i < candles.Count; i++)
        {
            var candle = candles[i];
            var prevCandle = candles[i - 1];

            var sma20 = candles.Skip(i - 20).Take(20).Average(c => c.Close);

            if (!inPosition && candle.Close >= sma20)
            {
                // Buy Signal
                inPosition = true;
                entryPrice = candle.Close;
                entryTime = candle.TimestampUtc;
                qty = (int)Math.Floor((currentCapital * 0.5m) / entryPrice);
                if (qty == 0) qty = 1;
            }
            else if (inPosition)
            {
                var stopLoss = entryPrice * 0.985m; // 1.5% stop loss
                var target = entryPrice * 1.03m;    // 3.0% target

                string? exitReason = null;
                if (candle.Low <= stopLoss)
                {
                    exitReason = "StopLoss";
                }
                else if (candle.High >= target)
                {
                    exitReason = "TargetReached";
                }
                else if (candle.Close < sma20)
                {
                    exitReason = "TrendReversal";
                }
                else if (i == candles.Count - 1)
                {
                    exitReason = "EndOfBacktest";
                }

                if (exitReason != null)
                {
                    var exitPrice = exitReason switch
                    {
                        "StopLoss" => stopLoss,
                        "TargetReached" => target,
                        _ => candle.Close
                    };

                    var gross = (exitPrice - entryPrice) * qty;
                    var charges = chargesCalculator.CalculateRoundTripCharges(ProductType.MIS, entryPrice, exitPrice, qty, request.Exchange);
                    var totalCharges = charges.TotalCharges;
                    var net = gross - totalCharges;

                    currentCapital += net;
                    if (currentCapital > peakCapital) peakCapital = currentCapital;
                    var dd = peakCapital - currentCapital;
                    if (dd > maxDrawdown) maxDrawdown = dd;

                    trades.Add(new BacktestTradeDto(
                        Side: "Buy",
                        Quantity: qty,
                        EntryPrice: entryPrice,
                        ExitPrice: exitPrice,
                        EntryTimeUtc: entryTime,
                        ExitTimeUtc: candle.TimestampUtc,
                        GrossPnL: Math.Round(gross, 2),
                        TotalCharges: Math.Round(totalCharges, 2),
                        NetPnL: Math.Round(net, 2),
                        ExitReason: exitReason
                    ));

                    inPosition = false;
                }
            }
        }

        var totalTrades = trades.Count;
        var winningTrades = trades.Count(t => t.NetPnL > 0);
        var losingTrades = trades.Count(t => t.NetPnL < 0);
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades * 100 : 0m;

        var winSum = trades.Where(t => t.NetPnL > 0).Sum(t => t.NetPnL);
        var lossSum = Math.Abs(trades.Where(t => t.NetPnL < 0).Sum(t => t.NetPnL));
        var profitFactor = lossSum > 0 ? Math.Round(winSum / lossSum, 2) : winSum > 0 ? 99.9m : 0m;

        var netPnL = currentCapital - request.InitialCapital;
        var returnPct = request.InitialCapital > 0 ? (netPnL / request.InitialCapital) * 100 : 0m;
        var maxDrawdownPct = request.InitialCapital > 0 ? (maxDrawdown / request.InitialCapital) * 100 : 0m;

        return new BacktestResultDto(
            StrategyDefinitionId: request.StrategyDefinitionId,
            Symbol: request.Symbol,
            FromUtc: request.FromUtc,
            ToUtc: request.ToUtc,
            InitialCapital: request.InitialCapital,
            FinalCapital: Math.Round(currentCapital, 2),
            NetPnL: Math.Round(netPnL, 2),
            ReturnPercentage: Math.Round(returnPct, 2),
            TotalTrades: totalTrades,
            WinningTrades: winningTrades,
            LosingTrades: losingTrades,
            WinRatePercentage: Math.Round(winRate, 1),
            MaxDrawdownPercentage: Math.Round(maxDrawdownPct, 2),
            ProfitFactor: profitFactor,
            Trades: trades
        );
    }
}
