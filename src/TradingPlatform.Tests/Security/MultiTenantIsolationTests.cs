using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Tests.Security;

public class MultiTenantIsolationTests
{
    private readonly Guid _userAId = Guid.NewGuid();
    private readonly Guid _userBId = Guid.NewGuid();

    private TradingDbContext CreateContext(Guid? currentUserId, bool isAdmin = false)
    {
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns(currentUserId);
        mockUser.Setup(u => u.IsAdmin).Returns(isAdmin);
        mockUser.Setup(u => u.IsAuthenticated).Returns(currentUserId.HasValue);

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"TradingDb_TenantTest_{Guid.NewGuid()}")
            .Options;

        return new TradingDbContext(options, mockUser.Object);
    }

    [Fact]
    public async Task QueryingOrders_WhenAuthenticatedAsUserA_ShouldOnlyReturnUserAOrders()
    {
        // Arrange: Seed database with orders for User A and User B using an Admin context
        var dbName = $"TradingDb_Isolation_{Guid.NewGuid()}";
        var adminMock = new Mock<ICurrentUserService>();
        adminMock.Setup(u => u.IsAdmin).Returns(true);
        adminMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var adminOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using (var seedContext = new TradingDbContext(adminOptions, adminMock.Object))
        {
            seedContext.TradingOrders.AddRange(
                new TradingOrder
                {
                    UserId = _userAId,
                    Quantity = 50,
                    Price = 1500m,
                    Side = OrderSide.Buy,
                    Status = OrderStatus.Filled
                },
                new TradingOrder
                {
                    UserId = _userAId,
                    Quantity = 20,
                    Price = 2800m,
                    Side = OrderSide.Buy,
                    Status = OrderStatus.Open
                },
                new TradingOrder
                {
                    UserId = _userBId,
                    Quantity = 100,
                    Price = 450m,
                    Side = OrderSide.Buy,
                    Status = OrderStatus.Filled
                }
            );
            await seedContext.SaveChangesAsync();
        }

        // Act: User A queries the database
        var userAMock = new Mock<ICurrentUserService>();
        userAMock.Setup(u => u.UserId).Returns(_userAId);
        userAMock.Setup(u => u.IsAdmin).Returns(false);
        userAMock.Setup(u => u.IsAuthenticated).Returns(true);

        var userAOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using var queryContext = new TradingDbContext(userAOptions, userAMock.Object);
        var visibleOrders = await queryContext.TradingOrders.ToListAsync();

        // Assert: User A sees only 2 orders, both belonging to User A
        visibleOrders.Should().HaveCount(2);
        visibleOrders.Should().OnlyContain(o => o.UserId == _userAId);
        visibleOrders.Any(o => o.UserId == _userBId).Should().BeFalse("Cross-tenant orders must be invisible");
    }

    [Fact]
    public async Task QueryingOrders_WhenAdmin_ShouldReturnAllUsersOrders()
    {
        // Arrange: Seed database with orders for User A and User B
        var dbName = $"TradingDb_Admin_{Guid.NewGuid()}";
        var adminMock = new Mock<ICurrentUserService>();
        adminMock.Setup(u => u.IsAdmin).Returns(true);
        adminMock.Setup(u => u.UserId).Returns(Guid.NewGuid());

        var adminOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using (var seedContext = new TradingDbContext(adminOptions, adminMock.Object))
        {
            seedContext.TradingOrders.AddRange(
                new TradingOrder { UserId = _userAId, Quantity = 10, Price = 100m },
                new TradingOrder { UserId = _userBId, Quantity = 20, Price = 200m }
            );
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert: Admin sees both orders
        using var queryContext = new TradingDbContext(adminOptions, adminMock.Object);
        var allOrders = await queryContext.TradingOrders.ToListAsync();

        allOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddingEntity_WithoutExplicitUserId_ShouldAutomaticallyStampCurrentUserId()
    {
        // Arrange
        var context = CreateContext(_userAId, isAdmin: false);
        var order = new TradingOrder
        {
            Quantity = 15,
            Price = 3200m,
            Side = OrderSide.Buy
        };

        // Act
        context.TradingOrders.Add(order);
        await context.SaveChangesAsync();

        // Assert
        order.UserId.Should().Be(_userAId, "DbContext interceptor must stamp tenant ID automatically");
    }
}
