using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Contracts.Risk;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Approvals;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.Security;

public class WorkerRestartRecoveryTests
{
    private TradingDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"WorkerRecoveryTest_{Guid.NewGuid()}")
            .Options;

        return new TradingDbContext(options, new DefaultCurrentUserService { UserId = userId, IsAdmin = true });
    }

    [Fact]
    public async Task WorkerRestart_WhenPendingApprovalsExist_ProcessesThemWithoutDuplicates()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        using var db = CreateDbContext(userId);

        var instrument = new Instrument
        {
            Id = instrumentId,
            TradingSymbol = "SBIN",
            CompanyName = "State Bank of India",
            Segment = "NSE_EQ",
            LotSize = 1,
            TickSize = 0.05m,
            IsActive = true
        };
        db.Instruments.Add(instrument);

        var provider = new BrokerProvider
        {
            Id = Guid.NewGuid(),
            Name = "Mock Engine",
            Code = BrokerCode.Mock,
            ApiBaseUrl = "http://localhost",
            IsActive = true
        };
        db.BrokerProviders.Add(provider);

        var connection = new BrokerConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BrokerProviderId = provider.Id,
            DisplayName = "Mock Connection",
            IsAuthenticated = true,
            IsActive = true
        };
        db.BrokerConnections.Add(connection);

        var account = new TradingAccount
        {
            Id = accountId,
            UserId = userId,
            BrokerConnectionId = connection.Id,
            AccountId = "ACC-REC-01",
            AccountName = "Recovery Account",
            AvailableFunds = 50000m,
            IsActive = true
        };
        db.TradingAccounts.Add(account);

        var approvalId = Guid.NewGuid();
        var expiredApproval = new PendingTradeApproval
        {
            Id = approvalId,
            UserId = userId,
            TradingAccountId = accountId,
            Symbol = "SBIN",
            CompanyName = "State Bank of India",
            Side = OrderSide.Buy,
            Quantity = 20,
            IndicativePrice = 800m,
            Status = ApprovalStatus.Pending,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddSeconds(-5),
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(-7)
        };
        db.PendingTradeApprovals.Add(expiredApproval);
        await db.SaveChangesAsync();

        var mockRisk = new Mock<IRiskValidationService>();
        mockRisk.Setup(r => r.ValidateOrderAsync(It.IsAny<OrderRiskEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskEvaluationResultDto(true, [], [], DateTime.UtcNow));

        var mockMarketData = new Mock<IMarketDataService>();
        mockMarketData.Setup(m => m.GetQuoteAsync("SBIN", "NSE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketQuoteDto("SBIN", "NSE", 800m, 0, 0, 800m, 800m, 800m, 800m, 10000, 799m, 10, 801m, 10, DateTime.UtcNow));

        var mockNotif = new Mock<ITradingNotificationService>();
        var brokerageCalculator = new IndianBrokerageCalculator();
        var positionService = new PositionService(db, brokerageCalculator, mockMarketData.Object, mockNotif.Object, new Mock<ILogger<PositionService>>().Object);

        var mockBroker = new MockBrokerAdapter();
        var brokerFactoryMock = new Mock<IBrokerAdapterFactory>();
        brokerFactoryMock.Setup(f => f.GetAdapter(It.IsAny<BrokerCode>())).Returns(mockBroker);

        var stateMachine = new OrderStateMachine();
        var orderService = new OrderService(db, stateMachine, positionService, brokerFactoryMock.Object, new Mock<ILogger<OrderService>>().Object);
        var mockLogger = new Mock<ILogger<TradeApprovalService>>();

        // First worker startup and execution
        var workerService1 = new TradeApprovalService(db, mockRisk.Object, orderService, mockNotif.Object, mockLogger.Object);
        var processedFirstRun = await workerService1.ProcessExpiredApprovalsAsync();

        processedFirstRun.Should().Be(1);
        var updated = await db.PendingTradeApprovals.FindAsync(approvalId);
        updated!.Status.Should().Be(ApprovalStatus.Approved);

        // Immediate simulated second reboot/cycle
        var workerService2 = new TradeApprovalService(db, mockRisk.Object, orderService, mockNotif.Object, mockLogger.Object);
        var processedSecondRun = await workerService2.ProcessExpiredApprovalsAsync();

        // Must be 0: No duplicate orders allowed
        processedSecondRun.Should().Be(0);

        var ordersCount = await db.TradingOrders.CountAsync(o => o.TradingAccountId == accountId && o.InstrumentId == instrumentId);
        ordersCount.Should().Be(1);
    }
}
