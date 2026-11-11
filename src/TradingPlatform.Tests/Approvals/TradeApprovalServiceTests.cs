using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Risk;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Approvals;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.Approvals;

public class TradeApprovalServiceTests
{
    private readonly TradingDbContext _dbContext;
    private readonly Mock<IRiskValidationService> _mockRiskService;
    private readonly Mock<ITradingNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<TradeApprovalService>> _mockLogger;
    private readonly TradeApprovalService _approvalService;

    private readonly Guid _testUserId;
    private readonly TradingAccount _testAccount;
    private readonly Instrument _testInstrument;

    public TradeApprovalServiceTests()
    {
        _testUserId = Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService { UserId = _testUserId, IsAdmin = true });

        _mockRiskService = new Mock<IRiskValidationService>();
        _mockNotificationService = new Mock<ITradingNotificationService>();
        _mockLogger = new Mock<ILogger<TradeApprovalService>>();

        var stateMachine = new OrderStateMachine();
        var brokerageCalculator = new IndianBrokerageCalculator();

        var mockMarketData = new Mock<IMarketDataService>();
        mockMarketData
            .Setup(m => m.GetQuoteAsync("INFY", "NSE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketQuoteDto(
                TradingSymbol: "INFY",
                Exchange: "NSE",
                LastPrice: 1850m,
                Change: 0,
                ChangePercent: 0,
                Open: 1850m,
                High: 1850m,
                Low: 1850m,
                PreviousClose: 1850m,
                Volume: 50000,
                BestBid: 1849m,
                BestBidQty: 100,
                BestAsk: 1851m,
                BestAskQty: 100,
                LastUpdatedUtc: DateTime.UtcNow
            ));

        var positionService = new PositionService(
            _dbContext,
            brokerageCalculator,
            mockMarketData.Object,
            _mockNotificationService.Object,
            new Mock<ILogger<PositionService>>().Object
        );

        var mockBroker = new MockBrokerAdapter();
        var brokerFactoryMock = new Mock<IBrokerAdapterFactory>();
        brokerFactoryMock.Setup(f => f.GetAdapter(It.IsAny<BrokerCode>())).Returns(mockBroker);

        var orderService = new OrderService(
            _dbContext,
            stateMachine,
            positionService,
            brokerFactoryMock.Object,
            new Mock<ILogger<OrderService>>().Object
        );

        _approvalService = new TradeApprovalService(
            _dbContext,
            _mockRiskService.Object,
            orderService,
            _mockNotificationService.Object,
            _mockLogger.Object
        );

        // Seed provider, connection, account config, account, instrument
        var provider = new BrokerProvider
        {
            Id = Guid.NewGuid(),
            Name = "Mock Engine",
            Code = BrokerCode.Mock,
            ApiBaseUrl = "http://localhost",
            IsActive = true
        };

        var connection = new BrokerConnection
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            BrokerProviderId = provider.Id,
            BrokerProvider = provider,
            DisplayName = "Paper Connection",
            IsAuthenticated = true,
            IsActive = true,
            TradingMode = TradingMode.Paper
        };

        var config = new AccountConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ApprovalDurationSeconds = 2,
            AutoExecuteOnTimeout = true
        };

        _testAccount = new TradingAccount
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            BrokerConnectionId = connection.Id,
            BrokerConnection = connection,
            AccountName = "Paper Account",
            AccountId = "MOCK-1",
            IsActive = true,
            TradingEnabled = true,
            TradingMode = TradingMode.Paper,
            AvailableFunds = 500000m,
            AllocatedCapital = 500000m,
            Configuration = config
        };

        _testInstrument = new Instrument
        {
            Id = Guid.NewGuid(),
            TradingSymbol = "INFY",
            CompanyName = "Infosys Ltd",
            Exchange = "NSE",
            IsActive = true
        };

        _dbContext.BrokerProviders.Add(provider);
        _dbContext.BrokerConnections.Add(connection);
        _dbContext.TradingAccounts.Add(_testAccount);
        _dbContext.Instruments.Add(_testInstrument);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateApproval_SetsServerSideDeadlineAndDispatchesNotification()
    {
        var request = new CreateTradeApprovalRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            TradingSignalId: null,
            Symbol: "INFY",
            CompanyName: "Infosys Ltd",
            Side: "Buy",
            Quantity: 10,
            IndicativePrice: 1850m,
            ApprovalDurationSeconds: 2
        );

        var before = DateTime.UtcNow;
        var approval = await _approvalService.CreateApprovalAsync(request);
        var after = DateTime.UtcNow;

        Assert.Equal("Pending", approval.Status);
        Assert.Equal("INFY", approval.Symbol);
        Assert.Equal(10, approval.Quantity);
        Assert.True(approval.ApprovalDeadlineUtc >= before.AddSeconds(1.9));
        Assert.True(approval.ApprovalDeadlineUtc <= after.AddSeconds(2.1));
        Assert.True(approval.RemainingSeconds <= 2.0 && approval.RemainingSeconds > 0);

        _mockNotificationService.Verify(
            n => n.PublishBuyApprovalRequestedAsync(It.Is<PendingTradeApprovalDto>(dto => dto.Id == approval.Id), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RejectApproval_WithinWindow_RejectsSuccessfullyAndPreventsOrder()
    {
        var request = new CreateTradeApprovalRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            TradingSignalId: null,
            Symbol: "INFY",
            CompanyName: "Infosys Ltd",
            Side: "Buy",
            Quantity: 10,
            IndicativePrice: 1850m,
            ApprovalDurationSeconds: 5 // 5-second window
        );

        var approval = await _approvalService.CreateApprovalAsync(request);

        // User clicks [REJECT] within window
        var decision = await _approvalService.RejectApprovalAsync(approval.Id, "Price too volatile", _testUserId);

        Assert.True(decision.Success);
        Assert.Equal("Rejected", decision.Status);

        var dbApproval = await _dbContext.PendingTradeApprovals.FindAsync(approval.Id);
        Assert.NotNull(dbApproval);
        Assert.Equal(ApprovalStatus.Rejected, dbApproval.Status);
        Assert.Equal("Price too volatile", dbApproval.DecisionReason);

        // Zero orders created
        var ordersCount = await _dbContext.TradingOrders.CountAsync();
        Assert.Equal(0, ordersCount);

        _mockNotificationService.Verify(
            n => n.PublishApprovalDecisionAsync(approval.Id, "Rejected", "Price too volatile", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RejectApproval_AfterDeadlinePassed_ReturnsExpiredFailure()
    {
        // 10 millisecond window
        var approvalEntity = new PendingTradeApproval
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            Symbol = "INFY",
            CompanyName = "Infosys Ltd",
            Side = OrderSide.Buy,
            Quantity = 10,
            IndicativePrice = 1850m,
            Status = ApprovalStatus.Pending,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddMilliseconds(-50) // already in past
        };

        _dbContext.PendingTradeApprovals.Add(approvalEntity);
        await _dbContext.SaveChangesAsync();

        var decision = await _approvalService.RejectApprovalAsync(approvalEntity.Id, "Late rejection", _testUserId);

        Assert.False(decision.Success);
        Assert.Equal("Expired", decision.Status);

        var updated = await _dbContext.PendingTradeApprovals.FindAsync(approvalEntity.Id);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Expired, updated.Status);
    }

    [Fact]
    public async Task ProcessExpiredApprovals_WhenAutoExecuteEnabled_RevalidatesRiskAndExecutesOrder()
    {
        // Approval whose 2-second countdown just completed
        var approvalEntity = new PendingTradeApproval
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            Symbol = "INFY",
            CompanyName = "Infosys Ltd",
            Side = OrderSide.Buy,
            Quantity = 10,
            IndicativePrice = 1850m,
            Status = ApprovalStatus.Pending,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddMilliseconds(-50)
        };

        _dbContext.PendingTradeApprovals.Add(approvalEntity);
        await _dbContext.SaveChangesAsync();

        // Risk check passes
        _mockRiskService
            .Setup(r => r.ValidateOrderAsync(It.IsAny<OrderRiskEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskEvaluationResultDto(true, [], [], DateTime.UtcNow));

        var processed = await _approvalService.ProcessExpiredApprovalsAsync();
        Assert.Equal(1, processed);

        var updated = await _dbContext.PendingTradeApprovals.FindAsync(approvalEntity.Id);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Approved, updated.Status);

        // Order was created and filled
        var order = await _dbContext.TradingOrders
            .FirstOrDefaultAsync(o => o.PendingTradeApprovalId == approvalEntity.Id);

        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(10, order.FilledQuantity);
        Assert.Equal(1850m, order.AveragePrice);

        _mockNotificationService.Verify(
            n => n.PublishApprovalDecisionAsync(approvalEntity.Id, "Approved", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ProcessExpiredApprovals_WhenRiskRevalidationFails_RejectsApprovalWithoutOrder()
    {
        var approvalEntity = new PendingTradeApproval
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            Symbol = "INFY",
            CompanyName = "Infosys Ltd",
            Side = OrderSide.Buy,
            Quantity = 10,
            IndicativePrice = 1850m,
            Status = ApprovalStatus.Pending,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddMilliseconds(-50)
        };

        _dbContext.PendingTradeApprovals.Add(approvalEntity);
        await _dbContext.SaveChangesAsync();

        // Risk check fails (e.g. daily loss breached or kill switch active)
        _mockRiskService
            .Setup(r => r.ValidateOrderAsync(It.IsAny<OrderRiskEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RiskEvaluationResultDto(
                false,
                [new RiskValidationFailureDto("RISK_009_DAILY_LOSS", "Daily loss limit breached", "Error")],
                [],
                DateTime.UtcNow
            ));

        var processed = await _approvalService.ProcessExpiredApprovalsAsync();
        Assert.Equal(1, processed);

        var updated = await _dbContext.PendingTradeApprovals.FindAsync(approvalEntity.Id);
        Assert.NotNull(updated);
        Assert.Equal(ApprovalStatus.Rejected, updated.Status);
        Assert.Contains("Daily loss limit breached", updated.DecisionReason);

        // Zero orders placed
        var ordersCount = await _dbContext.TradingOrders.CountAsync();
        Assert.Equal(0, ordersCount);
    }
}
