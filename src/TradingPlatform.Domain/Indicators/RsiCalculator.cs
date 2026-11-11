namespace TradingPlatform.Domain.Indicators;

public static class RsiCalculator
{
    public static decimal? CalculateLatest(IReadOnlyList<decimal> prices, int period = 14)
    {
        var series = CalculateSeries(prices, period);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<decimal?> CalculateSeries(IReadOnlyList<decimal> prices, int period = 14)
    {
        var result = new decimal?[prices.Count];
        if (prices == null || prices.Count <= period || period <= 0)
        {
            return result;
        }

        decimal gainSum = 0m;
        decimal lossSum = 0m;

        // Calculate initial period gains and losses
        for (int i = 1; i <= period; i++)
        {
            decimal diff = prices[i] - prices[i - 1];
            if (diff >= 0)
                gainSum += diff;
            else
                lossSum += Math.Abs(diff);
        }

        decimal avgGain = gainSum / period;
        decimal avgLoss = lossSum / period;

        result[period] = ComputeRsi(avgGain, avgLoss);

        // Wilder's smoothing for subsequent periods
        for (int i = period + 1; i < prices.Count; i++)
        {
            decimal diff = prices[i] - prices[i - 1];
            decimal currentGain = diff > 0 ? diff : 0m;
            decimal currentLoss = diff < 0 ? Math.Abs(diff) : 0m;

            avgGain = (avgGain * (period - 1) + currentGain) / period;
            avgLoss = (avgLoss * (period - 1) + currentLoss) / period;

            result[i] = ComputeRsi(avgGain, avgLoss);
        }

        return result;
    }

    private static decimal ComputeRsi(decimal avgGain, decimal avgLoss)
    {
        if (avgLoss == 0m)
        {
            return avgGain == 0m ? 50m : 100m;
        }

        decimal rs = avgGain / avgLoss;
        decimal rsi = 100m - (100m / (1m + rs));
        return Math.Round(rsi, 2);
    }
}
