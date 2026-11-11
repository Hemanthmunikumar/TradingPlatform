using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Domain.Indicators;
using TradingPlatform.Domain.Strategies;
using Xunit;

namespace TradingPlatform.Tests.Strategies;

public class TechnicalIndicatorTests
{
    [Fact]
    public void SmaCalculator_CalculatesCorrectAverage()
    {
        var prices = new List<decimal> { 10m, 11m, 12m, 13m, 14m };
        var sma = SmaCalculator.CalculateLatest(prices, 5);

        Assert.NotNull(sma);
        Assert.Equal(12.0m, sma.Value);
    }

    [Fact]
    public void EmaCalculator_AppliesExponentialWeighting()
    {
        // 5-period EMA on simple values
        var prices = new List<decimal> { 10m, 11m, 12m, 13m, 14m, 15m };
        var emaSeries = EmaCalculator.CalculateSeries(prices, 5);

        // SMA for first 5 is 12.0. Multiplier for period 5 is 2/(5+1) = 2/6 = 1/3.
        // EMA for index 5: (15 - 12) * (1/3) + 12 = 1.0 + 12 = 13.0
        Assert.NotNull(emaSeries[4]);
        Assert.Equal(12.0m, emaSeries[4]!.Value);

        Assert.NotNull(emaSeries[5]);
        Assert.Equal(13.0m, emaSeries[5]!.Value);
    }

    [Fact]
    public void RsiCalculator_WithMonotonicallyIncreasingPrices_ReturnsHighRsi()
    {
        // 20 consecutive gains
        var prices = new List<decimal>();
        for (int i = 0; i < 20; i++)
        {
            prices.Add(100m + i * 2m);
        }

        var rsi = RsiCalculator.CalculateLatest(prices, 14);

        Assert.NotNull(rsi);
        Assert.Equal(100.0m, rsi.Value);
    }

    [Fact]
    public void RsiCalculator_WithMonotonicallyDecreasingPrices_ReturnsLowRsi()
    {
        // 20 consecutive drops
        var prices = new List<decimal>();
        for (int i = 0; i < 20; i++)
        {
            prices.Add(200m - i * 2m);
        }

        var rsi = RsiCalculator.CalculateLatest(prices, 14);

        Assert.NotNull(rsi);
        Assert.Equal(0.0m, rsi.Value);
    }

    [Fact]
    public void MacdCalculator_CalculatesLinesAndHistogram()
    {
        var prices = new List<decimal>();
        // 40 price points
        for (int i = 0; i < 40; i++)
        {
            prices.Add(100m + (decimal)Math.Sin(i * 0.2) * 10m + i * 0.5m);
        }

        var macd = MacdCalculator.CalculateLatest(prices, 12, 26, 9);

        Assert.NotNull(macd);
        Assert.Equal(Math.Round(macd.MacdLine - macd.SignalLine, 4), macd.Histogram);
    }

    [Fact]
    public void VwapCalculator_CalculatesVolumeWeightedAverage()
    {
        var today = DateTime.UtcNow.Date;
        var bars = new List<OhlcvBar>
        {
            new(today.AddHours(9).AddMinutes(15), 100m, 102m, 98m, 100m, 1000), // typical = 100, PV = 100,000, Vol = 1,000
            new(today.AddHours(9).AddMinutes(20), 100m, 105m, 99m, 102m, 2000), // typical = 102, PV = 204,000, Vol = 2,000
        };

        var vwap = VwapCalculator.CalculateLatest(bars);

        // Total PV = 304,000, Total Vol = 3,000 -> VWAP = 304,000 / 3,000 = 101.3333
        Assert.NotNull(vwap);
        Assert.Equal(101.3333m, vwap.Value);
    }

    [Fact]
    public void BollingerBands_MaintainsBandOrdering()
    {
        var prices = new List<decimal>();
        for (int i = 0; i < 30; i++)
        {
            prices.Add(1500m + (i % 5) * 10m);
        }

        var bb = BollingerBandsCalculator.CalculateLatest(prices, 20, 2.0m);

        Assert.NotNull(bb);
        Assert.True(bb.UpperBand > bb.MiddleBand, "Upper band must be > Middle band");
        Assert.True(bb.MiddleBand > bb.LowerBand, "Middle band must be > Lower band");
        Assert.True(bb.Bandwidth > 0, "Bandwidth must be positive");
    }
}

public class StrategyEvaluationTests
{
    [Fact]
    public void EmaCrossoverStrategy_GeneratesBuySignal_OnBullishCrossover()
    {
        var strategy = new EmaCrossoverStrategy();
        var instrument = new Instrument
        {
            Id = Guid.NewGuid(),
            TradingSymbol = "INFY",
            CompanyName = "Infosys Limited",
            Exchange = "NSE"
        };
        var account = new TradingAccount
        {
            Id = Guid.NewGuid(),
            AccountName = "Primary Test Account",
            AllocatedCapital = 200000m,
            AvailableFunds = 200000m
        };
        var version = new StrategyVersion
        {
            Id = Guid.NewGuid(),
            RiskPerTradePercent = 1.0m,
            StopLossPercent = 1.0m,
            TargetPercent = 2.0m
        };

        // Create 40 candles engineered for a smooth bullish crossover on the last candle
        var candles = new List<OhlcvBar>();
        var baseDate = DateTime.UtcNow.AddHours(-4);

        // First 30 candles oscillate around 1800 with realistic up/down waves (RSI ~48-52)
        for (int i = 0; i < 30; i++)
        {
            decimal wave = (decimal)Math.Sin(i * 0.8) * 4m;
            decimal p = Math.Round(1800m + wave, 2);
            candles.Add(new OhlcvBar(
                baseDate.AddMinutes(i * 5),
                p - 1m,
                p + 2m,
                p - 2m,
                p,
                10000
            ));
        }

        // Next 15 candles gradually climbing so EMA 9 crosses above EMA 21
        for (int i = 1; i <= 15; i++)
        {
            decimal p = 1800m + (i * 2.5m);
            candles.Add(new OhlcvBar(
                baseDate.AddMinutes((30 + i) * 5),
                p - 1m,
                p + 2m,
                p - 2m,
                p,
                15000 + (i * 1000)
            ));
        }

        // Simulate incoming market candle stream and evaluate strategy bar-by-bar
        StrategyEvaluationResult? buySignal = null;

        for (int barCount = 25; barCount <= candles.Count; barCount++)
        {
            var currentStream = candles.Take(barCount).ToList();
            var currentContext = new StrategyContext(
                Instrument: instrument,
                Account: account,
                Version: version,
                Candles: currentStream,
                CurrentPrice: currentStream[^1].Close,
                TimestampUtc: DateTime.UtcNow
            );

            var eval = strategy.Evaluate(currentContext);
            if (eval.Action == SignalType.Buy)
            {
                buySignal = eval;
                break;
            }
        }

        Assert.NotNull(buySignal);
        Assert.Equal(SignalType.Buy, buySignal.Action);
        Assert.True(buySignal.SuggestedQuantity > 0, "Suggested quantity must be positive");
        Assert.NotNull(buySignal.StopLoss);
        Assert.NotNull(buySignal.Target);
        Assert.True(buySignal.StopLoss.Value < buySignal.Price, "Stop loss must be below entry price for BUY");
        Assert.True(buySignal.Target.Value > buySignal.Price, "Target must be above entry price for BUY");
        Assert.True(buySignal.Confidence >= 0.8m, "Confidence should be high for clean crossover");
    }
}
