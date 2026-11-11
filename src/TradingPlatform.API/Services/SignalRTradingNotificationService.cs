using Microsoft.AspNetCore.SignalR;
using TradingPlatform.API.Hubs;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Notifications;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.API.Services;

public class SignalRTradingNotificationService(
    IHubContext<TradingHub, ITradingHubClient> hubContext,
    ILogger<SignalRTradingNotificationService> logger) : ITradingNotificationService
{
    public async Task PublishBuyApprovalRequestedAsync(PendingTradeApprovalDto approval, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Broadcasting BuyApprovalRequested for {Symbol} (ID: {Id})", approval.Symbol, approval.Id);
            await hubContext.Clients.All.ReceiveBuyApproval(approval);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveBuyApproval for {ApprovalId}", approval.Id);
        }
    }

    public async Task PublishApprovalDecisionAsync(Guid approvalId, string status, string message, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Broadcasting ApprovalDecision for {ApprovalId}: {Status}", approvalId, status);
            await hubContext.Clients.All.ReceiveApprovalDecision(new { approvalId, status, message });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveApprovalDecision for {ApprovalId}", approvalId);
        }
    }

    public async Task PublishOrderSubmittedAsync(OrderDto order, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.All.ReceiveOrderSubmitted(order);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveOrderSubmitted for {OrderId}", order.Id);
        }
    }

    public async Task PublishOrderFilledAsync(OrderDto order, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.All.ReceiveOrderFilled(order);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveOrderFilled for {OrderId}", order.Id);
        }
    }

    public async Task PublishPositionClosedAsync(PositionDto position, CancellationToken ct = default)
    {
        try
        {
            await hubContext.Clients.All.ReceivePositionClosed(position);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceivePositionClosed for {Symbol}", position.Symbol);
        }
    }

    public async Task PublishSellExecutionAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Broadcasting SellExecutionToast for {Symbol}: NetPnL {PnL:F2}", notification.Symbol, notification.NetRealizedPnL);
            await hubContext.Clients.All.ReceiveSellExecutionToast(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveSellExecutionToast for {Symbol}", notification.Symbol);
        }
    }

    public async Task PublishStopLossHitAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            logger.LogWarning("SignalR Broadcasting StopLossHitToast for {Symbol}: NetPnL {PnL:F2}", notification.Symbol, notification.NetRealizedPnL);
            await hubContext.Clients.All.ReceiveStopLossHitToast(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveStopLossHitToast for {Symbol}", notification.Symbol);
        }
    }

    public async Task PublishTargetHitAsync(SellExecutionNotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Broadcasting TargetHitToast for {Symbol}: NetPnL +{PnL:F2}", notification.Symbol, notification.NetRealizedPnL);
            await hubContext.Clients.All.ReceiveTargetHitToast(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveTargetHitToast for {Symbol}", notification.Symbol);
        }
    }

    public async Task PublishRiskAlertAsync(RiskAlertNotificationDto alert, CancellationToken ct = default)
    {
        try
        {
            logger.LogWarning("SignalR Broadcasting RiskAlert [{Severity}] {Rule}: {Message}", alert.Severity, alert.RuleName, alert.Message);
            await hubContext.Clients.All.ReceiveRiskAlert(alert);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveRiskAlert");
        }
    }

    public async Task PublishSystemAlertAsync(SystemAlertNotificationDto alert, CancellationToken ct = default)
    {
        try
        {
            logger.LogWarning("SignalR Broadcasting SystemAlert [{Level}] {Title}: {Message}", alert.Level, alert.Title, alert.Message);
            await hubContext.Clients.All.ReceiveSystemAlert(alert);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveSystemAlert");
        }
    }

    public async Task SendUserNotificationAsync(Guid userId, NotificationDto notification, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Sending direct notification to user-{UserId}: [{Type}] {Title}", userId, notification.Type, notification.Title);
            await hubContext.Clients.Group($"user-{userId}").ReceiveNotification(notification);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send direct notification to user-{UserId}", userId);
        }
    }
}
