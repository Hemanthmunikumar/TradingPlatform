using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Notifications;
using TradingPlatform.Infrastructure.Persistence;
using Xunit;

namespace TradingPlatform.Tests.Notifications;

public class NotificationRepositoryTests
{
    private TradingDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TradingNotificationsTest_{Guid.NewGuid()}")
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(userId.HasValue);

        return new TradingDbContext(options, mockCurrentUserService.Object);
    }

    [Fact]
    public async Task CreateNotification_PersistsAndReturnsDto()
    {
        using var db = CreateDbContext();
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        var created = await repo.CreateNotificationAsync(
            userId: userId,
            type: "BUY_APPROVAL",
            title: "BUY RELIANCE Triggered",
            message: "50 shares @ Rs.2850.00",
            dataJson: "{\"symbol\":\"RELIANCE\"}"
        );

        created.Should().NotBeNull();
        created.UserId.Should().Be(userId);
        created.Type.Should().Be("BUY_APPROVAL");
        created.Title.Should().Be("BUY RELIANCE Triggered");
        created.IsRead.Should().BeFalse();

        var persisted = await db.Notifications.Include(n => n.Deliveries).FirstOrDefaultAsync(n => n.Id == created.Id);
        persisted.Should().NotBeNull();
        persisted!.Deliveries.Should().HaveCount(1);
        persisted.Deliveries.First().Channel.Should().Be(NotificationChannel.SignalR);
    }

    [Fact]
    public async Task GetUserNotifications_FiltersAndPaginatesCorrectly()
    {
        using var db = CreateDbContext();
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            await repo.CreateNotificationAsync(
                userId: userId,
                type: "SYSTEM_ALERT",
                title: $"Alert {i}",
                message: $"Details {i}"
            );
        }

        var (all, total) = await repo.GetUserNotificationsAsync(userId, page: 1, pageSize: 3);
        total.Should().Be(5);
        all.Should().HaveCount(3);

        var unreadCount = await repo.GetUnreadCountAsync(userId);
        unreadCount.Should().Be(5);
    }

    [Fact]
    public async Task MarkAsRead_UpdatesIsReadAndTimestamp()
    {
        using var db = CreateDbContext();
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        var notif = await repo.CreateNotificationAsync(userId, "TEST", "Title", "Msg");
        await repo.MarkAsReadAsync(notif.Id, userId);

        var unread = await repo.GetUnreadCountAsync(userId);
        unread.Should().Be(0);

        var updated = await db.Notifications.FindAsync(notif.Id);
        updated!.IsRead.Should().BeTrue();
        updated.ReadAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAllAsRead_MarksAllUserNotificationsAsRead()
    {
        using var db = CreateDbContext();
        var repo = new NotificationRepository(db);
        var userId = Guid.NewGuid();

        await repo.CreateNotificationAsync(userId, "TEST1", "T1", "M1");
        await repo.CreateNotificationAsync(userId, "TEST2", "T2", "M2");

        (await repo.GetUnreadCountAsync(userId)).Should().Be(2);

        await repo.MarkAllAsReadAsync(userId);

        (await repo.GetUnreadCountAsync(userId)).Should().Be(0);
    }
}
