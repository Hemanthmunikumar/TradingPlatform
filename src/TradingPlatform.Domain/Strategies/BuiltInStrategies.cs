using TradingPlatform.Domain.Enums;
using TradingPlatform.Domain.Indicators;

namespace TradingPlatform.Domain.Strategies;

public class EmaCrossoverStrategy : IStrategy
{
    public string Code => "EMA_CROSSOVER";
    public string Name => "Intraday EMA 9/21 Crossover with RSI Filter";
    public StrategyType StrategyType => StrategyType.Intraday;

    public StrategyEvaluationResult Evaluate(StrategyContext context)
    {
        var candles = context.Candles;
        if (candles == null || candles.Count < 30)
        {
            return new StrategyEvaluationResult(
                Action: SignalType.Hold,
                Price: context.CurrentPrice,
                SuggestedQuantity: 0,
                StopLoss: null,
                Target: null,
                Reason: "Insufficient historical candle bars for EMA calculation (minimum 30 required).",
                Confidence: 0m,
                Indicators: new IndicatorSnapshot(null, null, null, null, null, null, null, null, null, null)
            );
        }

        var closes = candles.Select(c => c.Close).ToList();
        var ema9Series = EmaCalculator.CalculateSeries(closes, 9);
        var ema21Series = EmaCalculator.CalculateSeries(closes, 21);
        var rsiSeries = RsiCalculator.CalculateSeries(closes, 14);
        var atrSeries = AtrCalculator.CalculateSeries(candles, 14);
        var macdSeries = MacdCalculator.CalculateSeries(closes);

        int lastIdx = candles.Count - 1;
        int prevIdx = candles.Count - 2;

        decimal? ema9Now = ema9Series[lastIdx];
        decimal? ema21Now = ema21Series[lastIdx];
        decimal? ema9Prev = ema9Series[prevIdx];
        decimal? ema21Prev = ema21Series[prevIdx];
        decimal? rsiNow = rsiSeries[lastIdx];
        decimal? atrNow = atrSeries[lastIdx];
        var macdNow = macdSeries[lastIdx];

        var snapshot = new IndicatorSnapshot(
            Sma20: SmaCalculator.CalculateLatest(closes, 20),
            Ema9: ema9Now,
            Ema21: ema21Now,
            Ema50: EmaCalculator.CalculateLatest(closes, 50),
            Ema200: null,
            Rsi14: rsiNow,
            Macd: macdNow,
            Vwap: VwapCalculator.CalculateLatest(candles),
            Atr14: atrNow,
            BollingerBands: BollingerBandsCalculator.CalculateLatest(closes, 20)
        );

        if (!ema9Now.HasValue || !ema21Now.HasValue || !ema9Prev.HasValue || !ema21Prev.HasValue)
        {
            return new StrategyEvaluationResult(
                SignalType.Hold, context.CurrentPrice, 0, null, null, 
                "EMA values not ready.", 0m, snapshot);
        }

        decimal price = context.CurrentPrice > 0 ? context.CurrentPrice : closes[^1];

        // Position sizing logic: allocate max risk (default 1% stop loss)
        decimal stopLossDistance = atrNow.HasValue && atrNow.Value > 0 
            ? Math.Max(atrNow.Value * 1.5m, price * 0.01m) 
            : price * 0.01m;

        decimal targetDistance = stopLossDistance * 2.0m; // 1:2 Risk to Reward

        // Calculate quantity respecting max capital per order
        decimal capital = context.Account.AllocatedCapital > 0 ? context.Account.AllocatedCapital : 100000m;
        decimal maxTradeValue = capital * (context.Version.RiskPerTradePercent / 100m) * 10m; // 10x leveraged or allocation
        int quantity = Math.Max(1, (int)(maxTradeValue / price));

        // 1. Bullish Crossover: EMA 9 crosses ABOVE EMA 21
        bool bullishCross = ema9Prev.Value <= ema21Prev.Value && ema9Now.Value > ema21Now.Value;
        if (bullishCross && (!rsiNow.HasValue || (rsiNow.Value >= 45m && rsiNow.Value <= 70m)))
        {
            decimal sl = Math.Round(price - stopLossDistance, 2);
            decimal tp = Math.Round(price + targetDistance, 2);

            return new StrategyEvaluationResult(
                Action: SignalType.Buy,
                Price: price,
                SuggestedQuantity: quantity,
                StopLoss: sl,
                Target: tp,
                Reason: $"Bullish EMA 9 ({ema9Now:F2}) crossed above EMA 21 ({ema21Now:F2}) with RSI {rsiNow:F1}.",
                Confidence: 0.85m,
                Indicators: snapshot
            );
        }

        // 2. Bearish Crossover: EMA 9 crosses BELOW EMA 21
        bool bearishCross = ema9Prev.Value >= ema21Prev.Value && ema9Now.Value < ema21Now.Value;
        if (bearishCross)
        {
            return new StrategyEvaluationResult(
                Action: SignalType.Sell,
                Price: price,
                SuggestedQuantity: quantity,
                StopLoss: Math.Round(price + stopLossDistance, 2),
                Target: Math.Round(price - targetDistance, 2),
                Reason: $"Bearish EMA 9 ({ema9Now:F2}) crossed below EMA 21 ({ema21Now:F2}).",
                Confidence: 0.80m,
                Indicators: snapshot
            );
        }

        return new StrategyEvaluationResult(
            Action: SignalType.Hold,
            Price: price,
            SuggestedQuantity: 0,
            StopLoss: null,
            Target: null,
            Reason: "No EMA crossover detected on current bar.",
            Confidence: 0m,
            Indicators: snapshot
        );
    }
}

public class VwapPullbackStrategy : IStrategy
{
    public string Code => "VWAP_PULLBACK";
    public string Name => "Intraday VWAP Pullback with Volume Confirmation";
    public StrategyType StrategyType => StrategyType.Intraday;

    public StrategyEvaluationResult Evaluate(StrategyContext context)
    {
        var candles = context.Candles;
        if (candles == null || candles.Count < 20)
        {
            return new StrategyEvaluationResult(
                SignalType.Hold, context.CurrentPrice, 0, null, null,
                "Insufficient candles for VWAP evaluation.", 0m,
                new IndicatorSnapshot(null, null, null, null, null, null, null, null, null, null)
            );
        }

        var closes = candles.Select(c => c.Close).ToList();
        var vwap = VwapCalculator.CalculateLatest(candles);
        var rsi = RsiCalculator.CalculateLatest(closes, 14);
        var atr = AtrCalculator.CalculateLatest(candles, 14);
        var sma20 = SmaCalculator.CalculateLatest(closes, 20);

        var snapshot = new IndicatorSnapshot(
            Sma20: sma20,
            Ema9: EmaCalculator.CalculateLatest(closes, 9),
            Ema21: EmaCalculator.CalculateLatest(closes, 21),
            Ema50: null,
            Ema200: null,
            Rsi14: rsi,
            Macd: MacdCalculator.CalculateLatest(closes),
            Vwap: vwap,
            Atr14: atr,
            BollingerBands: BollingerBandsCalculator.CalculateLatest(closes, 20)
        );

        if (!vwap.HasValue)
        {
            return new StrategyEvaluationResult(
                SignalType.Hold, context.CurrentPrice, 0, null, null,
                "VWAP not available.", 0m, snapshot);
        }

        decimal price = context.CurrentPrice > 0 ? context.CurrentPrice : closes[^1];
        decimal distanceToVwap = Math.Abs(price - vwap.Value) / vwap.Value;

        // Buy condition: Price is above VWAP but pulled back within 0.4%, with RSI between 40 and 65
        bool isPullbackNearVwap = price >= vwap.Value && distanceToVwap <= 0.004m;
        bool rsiHealthy = !rsi.HasValue || (rsi.Value >= 40m && rsi.Value <= 65m);

        decimal sl = atr.HasValue ? Math.Round(vwap.Value - (atr.Value * 1.2m), 2) : Math.Round(vwap.Value * 0.99m, 2);
        decimal tp = Math.Round(price + ((price - sl) * 2.0m), 2);

        decimal capital = context.Account.AllocatedCapital > 0 ? context.Account.AllocatedCapital : 100000m;
        int qty = Math.Max(1, (int)((capital * 0.1m) / price));

        if (isPullbackNearVwap && rsiHealthy)
        {
            return new StrategyEvaluationResult(
                Action: SignalType.Buy,
                Price: price,
                SuggestedQuantity: qty,
                StopLoss: sl,
                Target: tp,
                Reason: $"Price ({price:F2}) pulled back to VWAP ({vwap.Value:F2}) within {distanceToVwap * 100:F2}% with RSI {rsi:F1}.",
                Confidence: 0.88m,
                Indicators: snapshot
            );
        }

        // Sell condition: price broke cleanly below VWAP by more than 0.5%
        if (price < vwap.Value && (vwap.Value - price) / vwap.Value > 0.005m)
        {
            return new StrategyEvaluationResult(
                Action: SignalType.Sell,
                Price: price,
                SuggestedQuantity: qty,
                StopLoss: Math.Round(vwap.Value * 1.005m, 2),
                Target: Math.Round(price - (vwap.Value - price) * 1.5m, 2),
                Reason: $"Price ({price:F2}) broke below VWAP ({vwap.Value:F2}) by {((vwap.Value - price) / vwap.Value) * 100:F2}%.",
                Confidence: 0.75m,
                Indicators: snapshot
            );
        }

        return new StrategyEvaluationResult(
            Action: SignalType.Hold,
            Price: price,
            SuggestedQuantity: 0,
            StopLoss: null,
            Target: null,
            Reason: "Price not in VWAP pullback entry zone.",
            Confidence: 0m,
            Indicators: snapshot
        );
    }
}
