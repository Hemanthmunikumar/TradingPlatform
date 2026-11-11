namespace TradingPlatform.Domain.Indicators;

public static class VwapCalculator
{
    public static decimal? CalculateLatest(IReadOnlyList<OhlcvBar> bars)
    {
        var series = CalculateSeries(bars);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<decimal?> CalculateSeries(IReadOnlyList<OhlcvBar> bars)
    {
        var result = new decimal?[bars.Count];
        if (bars == null || bars.Count == 0)
        {
            return result;
        }

        decimal cumulativePv = 0m;
        long cumulativeVolume = 0;
        DateTime? currentDay = null;

        for (int i = 0; i < bars.Count; i++)
        {
            var bar = bars[i];
            // Reset VWAP at start of each trading day
            if (currentDay == null || bar.TimestampUtc.Date != currentDay.Value)
            {
                currentDay = bar.TimestampUtc.Date;
                cumulativePv = 0m;
                cumulativeVolume = 0;
            }

            decimal typicalPrice = (bar.High + bar.Low + bar.Close) / 3m;
            cumulativePv += typicalPrice * bar.Volume;
            cumulativeVolume += bar.Volume;

            if (cumulativeVolume > 0)
            {
                result[i] = Math.Round(cumulativePv / cumulativeVolume, 4);
            }
        }

        return result;
    }
}

public static class AtrCalculator
{
    public static decimal? CalculateLatest(IReadOnlyList<OhlcvBar> bars, int period = 14)
    {
        var series = CalculateSeries(bars, period);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<decimal?> CalculateSeries(IReadOnlyList<OhlcvBar> bars, int period = 14)
    {
        var result = new decimal?[bars.Count];
        if (bars == null || bars.Count <= period || period <= 0)
        {
            return result;
        }

        var trValues = new decimal[bars.Count];
        trValues[0] = bars[0].High - bars[0].Low;

        for (int i = 1; i < bars.Count; i++)
        {
            decimal hl = bars[i].High - bars[i].Low;
            decimal hcp = Math.Abs(bars[i].High - bars[i - 1].Close);
            decimal lcp = Math.Abs(bars[i].Low - bars[i - 1].Close);

            trValues[i] = Math.Max(hl, Math.Max(hcp, lcp));
        }

        // Initial ATR: SMA of initial period
        decimal sumTr = 0m;
        for (int i = 1; i <= period; i++)
        {
            sumTr += trValues[i];
        }

        decimal currentAtr = sumTr / period;
        result[period] = Math.Round(currentAtr, 4);

        // Wilder's smoothing
        for (int i = period + 1; i < bars.Count; i++)
        {
            currentAtr = (currentAtr * (period - 1) + trValues[i]) / period;
            result[i] = Math.Round(currentAtr, 4);
        }

        return result;
    }
}
