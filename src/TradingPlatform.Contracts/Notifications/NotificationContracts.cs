namespace TradingPlatform.Contracts.Notifications;

public interface ITradingHubClient
{
    Task ReceiveBuyApproval(object approval);
    Task ReceiveApprovalDecision(object decision);
    Task ReceiveOrderSubmitted(object order);
    Task ReceiveOrderFilled(object order);
    Task ReceivePositionClosed(object position);
    Task ReceiveSellExecutionToast(object toast);
    Task ReceiveStopLossHitToast(object toast);
    Task ReceiveTargetHitToast(object toast);
    Task ReceiveRiskAlert(object alert);
    Task ReceiveSystemAlert(object alert);
    Task ReceiveNotification(NotificationDto notification);
}

public record NotificationDto(
    Guid Id,
    Guid UserId,
    Guid? TradingAccountId,
    string Type,
    string Title,
    string Message,
    string? DataJson,
    bool IsRead,
    DateTime? ReadAtUtc,
    DateTime CreatedAtUtc
);

public record RiskAlertNotificationDto(
    Guid? TradingAccountId,
    string Severity,
    string RuleName,
    string Message,
    DateTime TimestampUtc
);

public record SystemAlertNotificationDto(
    string Level,
    string Title,
    string Message,
    DateTime TimestampUtc
);
