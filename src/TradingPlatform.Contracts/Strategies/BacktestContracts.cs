namespace TradingPlatform.Contracts.Strategies;

public record BacktestRequestDto(
    Guid StrategyDefinitionId,
    string Symbol,
    string Exchange,
    DateTime FromUtc,
    DateTime ToUtc,
    decimal InitialCapital,
    string Timeframe = "5m"
);

public record BacktestResultDto(
    Guid StrategyDefinitionId,
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    decimal InitialCapital,
    decimal FinalCapital,
    decimal NetPnL,
    decimal ReturnPercentage,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal WinRatePercentage,
    decimal MaxDrawdownPercentage,
    decimal ProfitFactor,
    IReadOnlyList<BacktestTradeDto> Trades
);

public record BacktestTradeDto(
    string Side,
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    DateTime EntryTimeUtc,
    DateTime ExitTimeUtc,
    decimal GrossPnL,
    decimal TotalCharges,
    decimal NetPnL,
    string ExitReason
);
