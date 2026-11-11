namespace TradingPlatform.Contracts.Analytics;

public record PortfolioAnalyticsDto(
    Guid UserId,
    Guid? TradingAccountId,
    decimal TotalPortfolioValue,
    decimal TotalRealizedPnL,
    decimal TotalUnrealizedPnL,
    decimal NetTotalPnL,
    decimal TodayPnL,
    decimal WeekPnL,
    decimal MonthPnL,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal WinRatePercentage,
    decimal AverageWinAmount,
    decimal AverageLossAmount,
    decimal ProfitFactor,
    decimal MaxDrawdownPercentage,
    IReadOnlyList<DailyPnLDataPointDto> DailyPnLTrend,
    IReadOnlyList<SymbolPnLSummaryDto> SymbolBreakdown,
    IReadOnlyList<StrategyPerformanceDto> StrategyBreakdown
);

public record DailyPnLDataPointDto(
    string Date, // yyyy-MM-dd
    decimal RealizedPnL,
    decimal CumulativePnL
);

public record SymbolPnLSummaryDto(
    string Symbol,
    int TradesCount,
    decimal TotalQuantity,
    decimal RealizedPnL,
    decimal TotalCharges,
    decimal NetPnL
);

public record StrategyPerformanceDto(
    string StrategyName,
    int TotalTrades,
    int WinningTrades,
    int LosingTrades,
    decimal WinRatePercentage,
    decimal RealizedNetPnL,
    decimal ProfitFactor
);
