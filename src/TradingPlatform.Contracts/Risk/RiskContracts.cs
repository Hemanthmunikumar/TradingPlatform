namespace TradingPlatform.Contracts.Risk;

public record OrderRiskEvaluationRequest(
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string Side, // "Buy" | "Sell"
    string OrderType, // "Market" | "Limit" | "StopLoss"
    string ProductType, // "CNC" | "MIS" | "NRML"
    int Quantity,
    decimal Price,
    decimal? StopLoss = null,
    decimal? Target = null
);

public record RiskValidationFailureDto(
    string RuleCode,
    string Message,
    string Severity // "Error" | "Warning"
);

public record RiskEvaluationResultDto(
    bool IsApproved,
    IReadOnlyList<RiskValidationFailureDto> Failures,
    IReadOnlyList<string> Warnings,
    DateTime EvaluatedAtUtc
);

public record DailyRiskSummaryDto(
    Guid TradingAccountId,
    string AccountName,
    decimal DailyLossLimit,
    decimal TodayRealizedPnL,
    decimal TodayUnrealizedPnL,
    decimal TotalNetPnL,
    decimal RemainingLossBuffer,
    int TradesCountToday,
    int MaxTradesPerDay,
    int OpenPositionsCount,
    int MaxPositions,
    bool KillSwitchActive
);

public record ToggleKillSwitchRequest(
    Guid? TradingAccountId,
    bool Activate,
    string Reason
);
