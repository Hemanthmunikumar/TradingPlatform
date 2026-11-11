using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Notifications;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Notifications;

public class NotificationRepository(TradingDbContext dbContext) : INotificationRepository
{
    public async Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? dataJson = null,
        Guid? tradingAccountId = null,
        CancellationToken ct = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TradingAccountId = tradingAccountId,
            Type = type,
            Title = title,
            Message = message,
            DataJson = dataJson,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            Deliveries = new List<NotificationDelivery>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Channel = NotificationChannel.SignalR,
                    Status = NotificationStatus.Sent,
                    SentAtUtc = DateTime.UtcNow
                }
            }
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(ct);

        return new NotificationDto(
            notification.Id,
            notification.UserId,
            notification.TradingAccountId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.DataJson,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedAtUtc
        );
    }

    public async Task<(IReadOnlyList<NotificationDto> Items, int TotalCount)> GetUserNotificationsAsync(
        Guid userId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.UserId,
                n.TradingAccountId,
                n.Type,
                n.Title,
                n.Message,
                n.DataJson,
                n.IsRead,
                n.ReadAtUtc,
                n.CreatedAtUtc
            ))
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (unread.Count > 0)
        {
            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAtUtc = now;
            }
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
