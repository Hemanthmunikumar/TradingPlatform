using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Risk;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Risk;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.Risk;

public class RiskValidationServiceTests
{
    private readonly TradingDbContext _dbContext;
    private readonly Mock<IMarketDataService> _mockMarketData;
    private readonly Mock<ILogger<RiskValidationService>> _mockLogger;
    private readonly RiskValidationService _riskService;

    private readonly TradingAccount _testAccount;
    private readonly Instrument _testInstrument;

    public RiskValidationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var testUserId = Guid.NewGuid();
        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService { UserId = testUserId, IsAdmin = true });
        _mockMarketData = new Mock<IMarketDataService>();
        _mockLogger = new Mock<ILogger<RiskValidationService>>();

        _riskService = new RiskValidationService(_dbContext, _mockMarketData.Object, _mockLogger.Object);

        // Setup default open market
        _mockMarketData
            .Setup(m => m.GetMarketStatus(It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(new MarketStatusDto(
                IsMarketOpen: true,
                CurrentSession: "Regular",
                Exchange: "NSE",
                ServerTimeIst: DateTime.UtcNow.AddHours(5.5),
                TimeToNextSession: TimeSpan.Zero
            ));

        _mockMarketData
            .Setup(m => m.GetQuoteAsync("INFY", "NSE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketQuoteDto(
                TradingSymbol: "INFY",
                Exchange: "NSE",
                LastPrice: 1850.0m,
                Change: 0,
                ChangePercent: 0,
                Open: 1850m,
                High: 1850m,
                Low: 1850m,
                PreviousClose: 1850m,
                Volume: 100000,
                BestBid: 1849.95m,
                BestBidQty: 100,
                BestAsk: 1850.05m,
                BestAskQty: 100,
                LastUpdatedUtc: DateTime.UtcNow
            ));

        // Seed test broker provider and connection
        var provider = new BrokerProvider
        {
            Id = Guid.NewGuid(),
            Name = "Mock Simulator",
            Code = BrokerCode.Mock,
            ApiBaseUrl = "http://localhost",
            IsActive = true
        };

        var connection = new BrokerConnection
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            BrokerProvider = provider,
            DisplayName = "Mock Test Connection",
            IsAuthenticated = true,
            IsActive = true,
            TradingMode = TradingMode.Paper
        };

        var config = new AccountConfiguration
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            MaxCapitalAllocation = 500000m,
            MaxOrderValue = 100000m,
            MaxPositions = 5,
            MaxRiskPerTradePercent = 2.0m,
            DailyLossLimit = 10000m,
            MaxExposurePerStockPercent = 25.0m,
            MaxTradesPerDay = 20,
            StopLossRequired = true,
            ApprovalDurationSeconds = 2,
            AutoExecuteOnTimeout = true,
            KillSwitchActive = false,
            TradingScheduleStart = new TimeOnly(0, 0), // Allow all day for test runner
            TradingScheduleEnd = new TimeOnly(23, 59),
            IntradaySquareOffTime = new TimeOnly(23, 58)
        };

        _testAccount = new TradingAccount
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            BrokerConnection = connection,
            AccountName = "Paper Primary Account",
            AccountId = "MOCK12345",
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
            CompanyName = "Infosys Limited",
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
    public async Task ValidateOrder_WhenOrderValueExceedsMaxOrderValue_ReturnsFailure()
    {
        // MaxOrderValue is 100,000. Order: 100 qty * 1,850 = 185,000
        var request = new OrderRiskEvaluationRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 100,
            Price: 1850m,
            StopLoss: 1830m,
            Target: 1890m
        );

        var result = await _riskService.ValidateOrderAsync(request);

        Assert.False(result.IsApproved);
        Assert.Contains(result.Failures, f => f.RuleCode == "RISK_012_ORDER_VALUE_LIMIT");
    }

    [Fact]
    public async Task ValidateOrder_WhenKillSwitchActive_ReturnsFailure()
    {
        _testAccount.Configuration!.KillSwitchActive = true;
        _dbContext.SaveChanges();

        var request = new OrderRiskEvaluationRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 10,
            Price: 1850m,
            StopLoss: 1830m,
            Target: 1890m
        );

        var result = await _riskService.ValidateOrderAsync(request);

        Assert.False(result.IsApproved);
        Assert.Contains(result.Failures, f => f.RuleCode == "RISK_002_ACCOUNT_KILL_SWITCH");
    }

    [Fact]
    public async Task ValidateOrder_WhenStopLossMissingAndRequired_ReturnsFailure()
    {
        var request = new OrderRiskEvaluationRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 10,
            Price: 1850m,
            StopLoss: null, // Missing mandatory stop loss
            Target: 1890m
        );

        var result = await _riskService.ValidateOrderAsync(request);

        Assert.False(result.IsApproved);
        Assert.Contains(result.Failures, f => f.RuleCode == "RISK_017_STOPLOSS_REQUIRED");
    }

    [Fact]
    public async Task ValidateOrder_WhenDuplicateOrderDetected_ReturnsFailure()
    {
        // Seed an order placed 2 seconds ago
        var recentOrder = new TradingOrder
        {
            Id = Guid.NewGuid(),
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            Quantity = 10,
            Price = 1850m,
            CreatedAtUtc = DateTime.UtcNow.AddSeconds(-2)
        };
        _dbContext.TradingOrders.Add(recentOrder);
        _dbContext.SaveChanges();

        var request = new OrderRiskEvaluationRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 10,
            Price: 1850m,
            StopLoss: 1830m,
            Target: 1890m
        );

        var result = await _riskService.ValidateOrderAsync(request);

        Assert.False(result.IsApproved);
        Assert.Contains(result.Failures, f => f.RuleCode == "RISK_018_DUPLICATE_ORDER");
    }

    [Fact]
    public async Task ValidateOrder_WhenCleanOrderMeetingAllRules_ReturnsApproved()
    {
        var request = new OrderRiskEvaluationRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 20, // 20 * 1850 = 37,000 (< 100,000 MaxOrderValue)
            Price: 1850m,
            StopLoss: 1835m, // Risk = (1850 - 1835) * 20 = 300 (< 2% of 500,000 = 10,000)
            Target: 1880m
        );

        var result = await _riskService.ValidateOrderAsync(request);

        Assert.True(result.IsApproved);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task ToggleKillSwitch_TogglesStateCorrectly()
    {
        var toggleReq = new ToggleKillSwitchRequest(
            TradingAccountId: _testAccount.Id,
            Activate: true,
            Reason: "Manual safety halt"
        );

        var success = await _riskService.ToggleKillSwitchAsync(toggleReq);

        Assert.True(success);
        var updatedAccount = await _dbContext.TradingAccounts
            .Include(a => a.Configuration)
            .FirstAsync(a => a.Id == _testAccount.Id);

        Assert.True(updatedAccount.Configuration!.KillSwitchActive);
    }
}
