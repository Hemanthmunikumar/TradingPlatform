namespace TradingPlatform.Contracts.Strategies;

public record MacdDto(
    decimal MacdLine,
    decimal SignalLine,
    decimal Histogram
);

public record BollingerBandsDto(
    decimal MiddleBand,
    decimal UpperBand,
    decimal LowerBand,
    decimal Bandwidth
);

public record IndicatorValuesDto(
    string Symbol,
    string Exchange,
    decimal CurrentPrice,
    decimal? Sma20,
    decimal? Ema9,
    decimal? Ema21,
    decimal? Ema50,
    decimal? Ema200,
    decimal? Rsi14,
    MacdDto? Macd,
    decimal? Vwap,
    decimal? Atr14,
    BollingerBandsDto? BollingerBands,
    DateTime CalculatedAtUtc
);

public record StrategyVersionDto(
    Guid Id,
    int VersionNumber,
    string ConfigurationJson,
    string? StockUniverseJson,
    decimal CapitalAllocation,
    int MaxPositions,
    decimal RiskPerTradePercent,
    decimal StopLossPercent,
    decimal TargetPercent,
    bool IsActive,
    IReadOnlyList<Guid> MappedAccountIds
);

public record StrategyDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string StrategyType,
    bool IsActive,
    StrategyVersionDto? CurrentVersion
);

public record CreateStrategyRequest(
    string Name,
    string? Description,
    string StrategyType, // "Intraday" | "Swing"
    string StrategyCode, // "EMA_CROSSOVER" | "VWAP_PULLBACK"
    decimal CapitalAllocation,
    decimal RiskPerTradePercent,
    decimal StopLossPercent,
    decimal TargetPercent,
    IReadOnlyList<Guid>? AccountIds = null
);

public record TradingSignalDto(
    Guid Id,
    Guid TradingAccountId,
    string AccountName,
    Guid InstrumentId,
    string Symbol,
    string CompanyName,
    string SignalType, // "Buy" | "Sell" | "Hold"
    decimal Price,
    int Quantity,
    decimal? StopLoss,
    decimal? Target,
    string Reason,
    decimal Confidence,
    string Status,
    DateTime CreatedAtUtc
);
