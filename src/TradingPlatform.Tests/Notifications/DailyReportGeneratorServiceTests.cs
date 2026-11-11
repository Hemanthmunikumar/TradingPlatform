using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Notifications;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Notifications;
using TradingPlatform.Infrastructure.Persistence;
using Xunit;

namespace TradingPlatform.Tests.Notifications;

public class DailyReportGeneratorServiceTests
{
    private TradingDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"DailyReportTest_{Guid.NewGuid()}")
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns(userId);
        mockCurrentUserService.Setup(s => s.IsAuthenticated).Returns(userId.HasValue);

        return new TradingDbContext(options, mockCurrentUserService.Object);
    }

    [Fact]
    public async Task GenerateDailyReport_ComputesPnLAndTradesCorrectly()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        using var db = CreateDbContext(userId);

        var user = new User
        {
            Id = userId,
            Email = "trader@example.com",
            FullName = "Pro Trader",
            PasswordHash = "hash",
            Timezone = "Asia/Kolkata"
        };
        db.Users.Add(user);

        var instrument = new Instrument
        {
            Id = instrumentId,
            TradingSymbol = "INFY",
            CompanyName = "Infosys Limited",
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
            AccountId = "ACC123",
            AccountName = "Primary Test",
            IsActive = true
        };
        db.TradingAccounts.Add(account);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var position = new Position
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TradingAccountId = accountId,
            InstrumentId = instrumentId,
            Instrument = instrument,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 25,
            AverageBuyPrice = 1820m,
            CurrentPrice = 1850m,
            RealizedGrossPnL = 750m,
            TotalCharges = 32.5m,
            RealizedNetPnL = 717.5m,
            IsOpen = false,
            OpenedAtUtc = DateTime.UtcNow.AddHours(-2),
            ClosedAtUtc = DateTime.UtcNow,
            ExitReason = "TargetReached"
        };
        db.Positions.Add(position);

        await db.SaveChangesAsync();

        var mockEmail = new Mock<IEmailService>();
        var mockWhatsApp = new Mock<IWhatsAppService>();
        var mockLogger = new Mock<ILogger<DailyReportGeneratorService>>();

        var service = new DailyReportGeneratorService(db, mockEmail.Object, mockWhatsApp.Object, mockLogger.Object);

        var report = await service.GenerateDailyReportAsync(userId, today, accountId);

        report.Should().NotBeNull();
        report.TotalTrades.Should().Be(1);
        report.WinningTrades.Should().Be(1);
        report.LosingTrades.Should().Be(0);
        report.NetRealizedPnL.Should().Be(717.5m);
        report.TotalCharges.Should().Be(32.5m);
        report.GrossPnL.Should().Be(750m);
        report.WinRatePercentage.Should().Be(100m);
        report.Trades.Should().HaveCount(1);
        report.Trades[0].Symbol.Should().Be("INFY");
    }

    [Fact]
    public async Task DispatchDailyReports_SendsEmailAndWhatsApp()
    {
        var userId = Guid.NewGuid();
        using var db = CreateDbContext(userId);

        var user = new User
        {
            Id = userId,
            Email = "digest@example.com",
            PhoneNumber = "+919876543210",
            FullName = "Automated Digest User",
            PasswordHash = "hash"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockEmail = new Mock<IEmailService>();
        mockEmail.Setup(m => m.SendDailyReportEmailAsync(It.IsAny<string>(), It.IsAny<DailyTradingReportDto>(), default))
            .ReturnsAsync(true);

        var mockWhatsApp = new Mock<IWhatsAppService>();
        mockWhatsApp.Setup(m => m.SendDailyReportWhatsAppAsync(It.IsAny<string>(), It.IsAny<DailyTradingReportDto>(), default))
            .ReturnsAsync(true);

        var mockLogger = new Mock<ILogger<DailyReportGeneratorService>>();

        var service = new DailyReportGeneratorService(db, mockEmail.Object, mockWhatsApp.Object, mockLogger.Object);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await service.DispatchDailyReportsAsync(today);

        mockEmail.Verify(m => m.SendDailyReportEmailAsync("digest@example.com", It.IsAny<DailyTradingReportDto>(), default), Times.Once);
        mockWhatsApp.Verify(m => m.SendDailyReportWhatsAppAsync("+919876543210", It.IsAny<DailyTradingReportDto>(), default), Times.Once);
    }
}
