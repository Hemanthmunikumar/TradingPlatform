using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Interfaces.Notifications;

public interface ITradingNotificationService
{
    Task PublishBuyApprovalRequestedAsync(PendingTradeApprovalDto approval, CancellationToken ct = default);
    Task PublishApprovalDecisionAsync(Guid approvalId, string status, string message, CancellationToken ct = default);
    Task PublishOrderSubmittedAsync(OrderDto order, CancellationToken ct = default);
    Task PublishOrderFilledAsync(OrderDto order, CancellationToken ct = default);
    Task PublishPositionClosedAsync(PositionDto position, CancellationToken ct = default);
}
