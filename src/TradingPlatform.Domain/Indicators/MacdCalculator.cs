namespace TradingPlatform.Domain.Indicators;

public static class MacdCalculator
{
    public static MacdResult? CalculateLatest(
        IReadOnlyList<decimal> prices, 
        int fastPeriod = 12, 
        int slowPeriod = 26, 
        int signalPeriod = 9)
    {
        var series = CalculateSeries(prices, fastPeriod, slowPeriod, signalPeriod);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<MacdResult?> CalculateSeries(
        IReadOnlyList<decimal> prices, 
        int fastPeriod = 12, 
        int slowPeriod = 26, 
        int signalPeriod = 9)
    {
        var result = new MacdResult?[prices.Count];
        int minRequired = slowPeriod + signalPeriod;
        if (prices == null || prices.Count < minRequired)
        {
            return result;
        }

        var fastEma = EmaCalculator.CalculateSeries(prices, fastPeriod);
        var slowEma = EmaCalculator.CalculateSeries(prices, slowPeriod);

        // MACD Line = Fast EMA - Slow EMA
        var macdLine = new decimal?[prices.Count];
        var macdValidValues = new List<decimal>();
        var macdValidIndices = new List<int>();

        for (int i = slowPeriod - 1; i < prices.Count; i++)
        {
            if (fastEma[i].HasValue && slowEma[i].HasValue)
            {
                var val = fastEma[i]!.Value - slowEma[i]!.Value;
                macdLine[i] = val;
                macdValidValues.Add(val);
                macdValidIndices.Add(i);
            }
        }

        if (macdValidValues.Count < signalPeriod)
        {
            return result;
        }

        // Signal Line = EMA of MACD Line
        var signalSeries = EmaCalculator.CalculateSeries(macdValidValues, signalPeriod);

        for (int j = 0; j < signalSeries.Count; j++)
        {
            if (signalSeries[j].HasValue)
            {
                int originalIndex = macdValidIndices[j];
                var mLine = macdLine[originalIndex]!.Value;
                var sLine = signalSeries[j]!.Value;
                var hist = mLine - sLine;

                result[originalIndex] = new MacdResult(
                    MacdLine: Math.Round(mLine, 4),
                    SignalLine: Math.Round(sLine, 4),
                    Histogram: Math.Round(hist, 4)
                );
            }
        }

        return result;
    }
}
