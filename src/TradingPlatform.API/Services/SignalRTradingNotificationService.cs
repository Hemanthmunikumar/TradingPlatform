using Microsoft.AspNetCore.SignalR;
using TradingPlatform.API.Hubs;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.API.Services;

public class SignalRTradingNotificationService(
    IHubContext<TradingHub> hubContext,
    ILogger<SignalRTradingNotificationService> logger) : ITradingNotificationService
{
    public async Task PublishBuyApprovalRequestedAsync(PendingTradeApprovalDto approval, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("SignalR Broadcasting BuyApprovalRequested for {Symbol} (ID: {Id})", approval.Symbol, approval.Id);
            await hubContext.Clients.All.SendAsync("ReceiveBuyApproval", approval, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveApprovalDecision", new { approvalId, status, message }, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveOrderSubmitted", order, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveOrderFilled", order, ct);
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
            await hubContext.Clients.All.SendAsync("ReceivePositionClosed", position, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveSellExecutionToast", notification, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveStopLossHitToast", notification, ct);
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
            await hubContext.Clients.All.SendAsync("ReceiveTargetHitToast", notification, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to broadcast ReceiveTargetHitToast for {Symbol}", notification.Symbol);
        }
    }
}
