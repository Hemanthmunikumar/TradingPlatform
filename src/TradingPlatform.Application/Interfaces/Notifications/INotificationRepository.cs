using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.Application.Interfaces.Notifications;

public interface INotificationRepository
{
    Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? dataJson = null,
        Guid? tradingAccountId = null,
        CancellationToken ct = default);

    Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid userId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
