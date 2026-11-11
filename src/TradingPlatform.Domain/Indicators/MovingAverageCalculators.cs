namespace TradingPlatform.Domain.Indicators;

public static class SmaCalculator
{
    public static decimal? CalculateLatest(IReadOnlyList<decimal> prices, int period)
    {
        if (prices == null || prices.Count < period || period <= 0)
        {
            return null;
        }

        decimal sum = 0m;
        for (int i = prices.Count - period; i < prices.Count; i++)
        {
            sum += prices[i];
        }

        return Math.Round(sum / period, 4);
    }

    public static IReadOnlyList<decimal?> CalculateSeries(IReadOnlyList<decimal> prices, int period)
    {
        var result = new decimal?[prices.Count];
        if (prices.Count < period || period <= 0)
        {
            return result;
        }

        decimal sum = 0m;
        for (int i = 0; i < prices.Count; i++)
        {
            sum += prices[i];
            if (i >= period)
            {
                sum -= prices[i - period];
            }

            if (i >= period - 1)
            {
                result[i] = Math.Round(sum / period, 4);
            }
        }

        return result;
    }
}

public static class EmaCalculator
{
    public static decimal? CalculateLatest(IReadOnlyList<decimal> prices, int period)
    {
        var series = CalculateSeries(prices, period);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<decimal?> CalculateSeries(IReadOnlyList<decimal> prices, int period)
    {
        var result = new decimal?[prices.Count];
        if (prices == null || prices.Count < period || period <= 0)
        {
            return result;
        }

        // Multiplier: 2 / (period + 1)
        decimal multiplier = 2m / (period + 1m);

        // Initial SMA for first period
        decimal sum = 0m;
        for (int i = 0; i < period; i++)
        {
            sum += prices[i];
        }

        decimal currentEma = sum / period;
        result[period - 1] = Math.Round(currentEma, 4);

        for (int i = period; i < prices.Count; i++)
        {
            currentEma = (prices[i] - currentEma) * multiplier + currentEma;
            result[i] = Math.Round(currentEma, 4);
        }

        return result;
    }
}
