using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Notifications;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.Infrastructure.Notifications;

public class LoggingTradingNotificationService(ILogger<LoggingTradingNotificationService> logger) : ITradingNotificationService
{
    public Task PublishBuyApprovalRequestedAsync(PendingTradeApprovalDto approval, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: BUY Approval Requested for {Symbol} (Qty: {Qty}, Price: {Price}). Expires in {Sec}s. ID: {Id}",
            approval.Symbol, approval.Quantity, approval.IndicativePrice, approval.RemainingSeconds, approval.Id);
        return Task.CompletedTask;
    }

    public Task PublishApprovalDecisionAsync(Guid approvalId, string status, string message, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Approval {Id} decision: {Status}. {Message}", approvalId, status, message);
        return Task.CompletedTask;
    }

    public Task PublishOrderSubmittedAsync(OrderDto order, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Order Submitted {OrderId} ({Side} {Qty} {Symbol})", order.Id, order.Side, order.Quantity, order.Symbol);
        return Task.CompletedTask;
    }

    public Task PublishOrderFilledAsync(OrderDto order, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Order Filled {OrderId} ({Side} {Qty} {Symbol} @ {Price})", order.Id, order.Side, order.Quantity, order.Symbol, order.AveragePrice);
        return Task.CompletedTask;
    }

    public Task PublishPositionClosedAsync(PositionDto position, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: Position Closed {Symbol} RealizedNetPnL: {PnL:F2}, TotalCharges: {Charges:F2}",
            position.Symbol, position.RealizedNetPnL, position.TotalCharges);
        return Task.CompletedTask;
    }

    public Task PublishSellExecutionAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: SELL Executed for {Symbol} (Qty: {Qty}, Exit: {ExitPrice}, NetPnL: {NetPnL:F2}, Duration: {Duration})",
            notification.Symbol, notification.Quantity, notification.ExitPrice, notification.NetRealizedPnL, notification.HoldDuration);
        return Task.CompletedTask;
    }

    public Task PublishStopLossHitAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        logger.LogWarning("NOTIFICATION: STOP LOSS HIT for {Symbol}! Exit: {ExitPrice}, NetPnL: {NetPnL:F2}",
            notification.Symbol, notification.ExitPrice, notification.NetRealizedPnL);
        return Task.CompletedTask;
    }

    public Task PublishTargetHitAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: TARGET REACHED for {Symbol}! Exit: {ExitPrice}, NetPnL: +{NetPnL:F2}",
            notification.Symbol, notification.ExitPrice, notification.NetRealizedPnL);
        return Task.CompletedTask;
    }

    public Task PublishRiskAlertAsync(RiskAlertNotificationDto alert, CancellationToken ct = default)
    {
        logger.LogWarning("NOTIFICATION: RISK ALERT [{Severity}] {Rule}: {Message}", alert.Severity, alert.RuleName, alert.Message);
        return Task.CompletedTask;
    }

    public Task PublishSystemAlertAsync(SystemAlertNotificationDto alert, CancellationToken ct = default)
    {
        logger.LogWarning("NOTIFICATION: SYSTEM ALERT [{Level}] {Title}: {Message}", alert.Level, alert.Title, alert.Message);
        return Task.CompletedTask;
    }

    public Task SendUserNotificationAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        logger.LogInformation("NOTIFICATION: USER {UserId} -> [{Type}] {Title}", userId, notification.Type, notification.Title);
        return Task.CompletedTask;
    }
}
