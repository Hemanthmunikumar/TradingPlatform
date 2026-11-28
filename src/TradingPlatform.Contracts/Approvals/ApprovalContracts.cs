namespace TradingPlatform.Contracts.Approvals;

public record PendingTradeApprovalDto(
    Guid Id,
    Guid TradingAccountId,
    Guid? TradingSignalId,
    string Symbol,
    string CompanyName,
    string Side,
    int Quantity,
    decimal IndicativePrice,
    decimal EstimatedOrderValue,
    decimal? StopLoss,
    decimal? Target,
    decimal? RiskAmount,
    string Reason,
    DateTime ApprovalDeadlineUtc,
    string Status,
    DateTime? DecidedAtUtc,
    string? DecisionReason,
    double RemainingSeconds,
    bool IsExpired,
    DateTime? CreatedAtUtc = null
);

public record RejectApprovalRequest(
    string? Reason = "User rejected within rejection window"
);

public record CreateTradeApprovalRequest(
    Guid TradingAccountId,
    Guid InstrumentId,
    Guid? TradingSignalId,
    string Symbol,
    string CompanyName,
    string Side,
    int Quantity,
    decimal IndicativePrice,
    decimal? StopLoss = null,
    decimal? Target = null,
    decimal? RiskAmount = null,
    string Reason = "Strategy Signal Triggered",
    int ApprovalDurationSeconds = 2
);

public record ApprovalDecisionResultDto(
    bool Success,
    string Message,
    string Status,
    DateTime TimestampUtc
);
