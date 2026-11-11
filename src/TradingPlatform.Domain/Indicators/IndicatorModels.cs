namespace TradingPlatform.Domain.Indicators;

public record OhlcvBar(
    DateTime TimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);

public record MacdResult(
    decimal MacdLine,
    decimal SignalLine,
    decimal Histogram
);

public record BollingerBandsResult(
    decimal MiddleBand,
    decimal UpperBand,
    decimal LowerBand,
    decimal Bandwidth
);

public record IndicatorSnapshot(
    decimal? Sma20,
    decimal? Ema9,
    decimal? Ema21,
    decimal? Ema50,
    decimal? Ema200,
    decimal? Rsi14,
    MacdResult? Macd,
    decimal? Vwap,
    decimal? Atr14,
    BollingerBandsResult? BollingerBands
);
