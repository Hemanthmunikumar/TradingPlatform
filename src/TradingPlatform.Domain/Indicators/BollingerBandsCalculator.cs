namespace TradingPlatform.Domain.Indicators;

public static class BollingerBandsCalculator
{
    public static BollingerBandsResult? CalculateLatest(
        IReadOnlyList<decimal> prices, 
        int period = 20, 
        decimal standardDeviations = 2.0m)
    {
        var series = CalculateSeries(prices, period, standardDeviations);
        return series.Count > 0 ? series[^1] : null;
    }

    public static IReadOnlyList<BollingerBandsResult?> CalculateSeries(
        IReadOnlyList<decimal> prices, 
        int period = 20, 
        decimal standardDeviations = 2.0m)
    {
        var result = new BollingerBandsResult?[prices.Count];
        if (prices == null || prices.Count < period || period <= 0)
        {
            return result;
        }

        var smaSeries = SmaCalculator.CalculateSeries(prices, period);

        for (int i = period - 1; i < prices.Count; i++)
        {
            if (!smaSeries[i].HasValue) continue;

            decimal mean = smaSeries[i]!.Value;
            decimal sumSquaredDiffs = 0m;

            for (int j = i - period + 1; j <= i; j++)
            {
                decimal diff = prices[j] - mean;
                sumSquaredDiffs += diff * diff;
            }

            double variance = (double)(sumSquaredDiffs / period);
            decimal stdDev = (decimal)Math.Sqrt(variance);

            decimal upper = mean + (standardDeviations * stdDev);
            decimal lower = mean - (standardDeviations * stdDev);
            decimal bandwidth = mean > 0 ? (upper - lower) / mean : 0m;

            result[i] = new BollingerBandsResult(
                MiddleBand: Math.Round(mean, 4),
                UpperBand: Math.Round(upper, 4),
                LowerBand: Math.Round(lower, 4),
                Bandwidth: Math.Round(bandwidth, 4)
            );
        }

        return result;
    }
}
