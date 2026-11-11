using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Contracts.Analytics;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Analytics;
using TradingPlatform.Infrastructure.Persistence;
using Xunit;

namespace TradingPlatform.Tests.Analytics;

public class AnalyticsServiceTests
{
    private TradingDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"AnalyticsTest_{Guid.NewGuid()}")
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(userId.HasValue);

        return new TradingDbContext(options, mockCurrentUserService.Object);
    }

    [Fact]
    public async Task GetPortfolioAnalytics_ComputesMetricsAccurately()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        using var db = CreateDbContext(userId);

        var instrument = new Instrument
        {
            Id = instrumentId,
            TradingSymbol = "TCS",
            CompanyName = "Tata Consultancy Services",
            Segment = "NSE_EQ",
            LotSize = 1,
            TickSize = 0.05m,
            IsActive = true
        };
        db.Instruments.Add(instrument);

        var account = new TradingAccount
        {
            Id = accountId,
            UserId = userId,
            BrokerConnectionId = Guid.NewGuid(),
            AccountId = "ACC999",
            AccountName = "Algo Main",
            AvailableFunds = 50000m,
            AllocatedCapital = 100000m,
            IsActive = true
        };
        db.TradingAccounts.Add(account);

        // Position 1: Closed Profitable Position
        var pos1 = new Position
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TradingAccountId = accountId,
            InstrumentId = instrumentId,
            Instrument = instrument,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 10,
            AverageBuyPrice = 3500m,
            CurrentPrice = 3600m,
            RealizedGrossPnL = 1000m,
            TotalCharges = 50m,
            RealizedNetPnL = 950m,
            IsOpen = false,
            OpenedAtUtc = DateTime.UtcNow.AddDays(-2),
            ClosedAtUtc = DateTime.UtcNow.AddHours(-3),
            ExitReason = "TargetReached"
        };
        db.Positions.Add(pos1);

        // Position 2: Open Position with Unrealized PnL
        var pos2 = new Position
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TradingAccountId = accountId,
            InstrumentId = instrumentId,
            Instrument = instrument,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 5,
            AverageBuyPrice = 3500m,
            CurrentPrice = 3520m, // (3520-3500)*5 = 100 unrealized
            IsOpen = true,
            OpenedAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.Positions.Add(pos2);

        await db.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<AnalyticsService>>();
        var service = new AnalyticsService(db, mockLogger.Object);

        var analytics = await service.GetPortfolioAnalyticsAsync(userId, accountId);

        analytics.Should().NotBeNull();
        analytics.TotalRealizedPnL.Should().Be(950m);
        analytics.TotalUnrealizedPnL.Should().Be(100m);
        analytics.NetTotalPnL.Should().Be(1050m);
        analytics.TotalTrades.Should().Be(1);
        analytics.WinningTrades.Should().Be(1);
        analytics.LosingTrades.Should().Be(0);
        analytics.WinRatePercentage.Should().Be(100m);
        analytics.ProfitFactor.Should().Be(99.9m); // No losses

        analytics.SymbolBreakdown.Should().HaveCount(1);
        analytics.SymbolBreakdown[0].Symbol.Should().Be("TCS");
        analytics.SymbolBreakdown[0].NetPnL.Should().Be(950m);

        analytics.DailyPnLTrend.Should().NotBeEmpty();
    }
}
