using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.Orders;

public class OrderServicePaperTradingTests
{
    private readonly TradingDbContext _dbContext;
    private readonly OrderStateMachine _stateMachine;
    private readonly IndianBrokerageCalculator _brokerageCalculator;
    private readonly Mock<IMarketDataService> _mockMarketData;
    private readonly PositionService _positionService;
    private readonly OrderService _orderService;

    private readonly Guid _testUserId;
    private readonly TradingAccount _testAccount;
    private readonly Instrument _testInstrument;

    public OrderServicePaperTradingTests()
    {
        _testUserId = Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService { UserId = _testUserId, IsAdmin = true });

        _stateMachine = new OrderStateMachine();
        _brokerageCalculator = new IndianBrokerageCalculator();
        _mockMarketData = new Mock<IMarketDataService>();

        _mockMarketData
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

        var posLogger = new Mock<ILogger<PositionService>>();
        _positionService = new PositionService(_dbContext, _brokerageCalculator, _mockMarketData.Object, posLogger.Object);

        var mockBroker = new MockBrokerAdapter();
        var brokerFactoryMock = new Mock<IBrokerAdapterFactory>();
        brokerFactoryMock.Setup(f => f.GetAdapter(It.IsAny<BrokerCode>())).Returns(mockBroker);

        var orderLogger = new Mock<ILogger<OrderService>>();
        _orderService = new OrderService(_dbContext, _stateMachine, _positionService, brokerFactoryMock.Object, orderLogger.Object);

        // Seed BrokerProvider, Connection, Account, Instrument
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
            AllocatedCapital = 500000m
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
    public async Task PaperTrade_BuyAndSellLifecycle_CreatesPositionAndCalculatesNetPnL()
    {
        // 1. Create BUY Order for INFY (10 shares @ ₹1,850 = ₹18,500)
        var buyRequest = new CreateOrderRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Buy",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 10,
            Price: 1850m
        );

        var createdBuy = await _orderService.CreateOrderAsync(buyRequest);
        Assert.Equal("Created", createdBuy.Status);

        // 2. Submit BUY Order -> Paper trading automatically executes and fills
        var filledBuy = await _orderService.SubmitOrderAsync(createdBuy.Id);
        Assert.Equal("Filled", filledBuy.Status);
        Assert.Equal(10, filledBuy.FilledQuantity);
        Assert.Equal(1850m, filledBuy.AveragePrice);

        // 3. Verify Position is opened
        var openPositions = await _positionService.GetPositionsAsync(_testAccount.Id, openOnly: true);
        Assert.Single(openPositions);
        var position = openPositions[0];
        Assert.Equal("INFY", position.Symbol);
        Assert.Equal(10, position.Quantity);
        Assert.Equal(1850m, position.AverageBuyPrice);
        Assert.True(position.IsOpen);
        Assert.True(position.TotalCharges > 0);

        // 4. Create SELL Order to close position (10 shares @ ₹1,890 = ₹18,900)
        // Expected Gross P&L: (1,890 - 1,850) * 10 = ₹400.00
        var sellRequest = new CreateOrderRequest(
            TradingAccountId: _testAccount.Id,
            InstrumentId: _testInstrument.Id,
            Symbol: "INFY",
            Side: "Sell",
            OrderType: "Limit",
            ProductType: "MIS",
            Quantity: 10,
            Price: 1890m
        );

        var createdSell = await _orderService.CreateOrderAsync(sellRequest);
        var filledSell = await _orderService.SubmitOrderAsync(createdSell.Id);
        Assert.Equal("Filled", filledSell.Status);
        Assert.Equal(10, filledSell.FilledQuantity);

        // 5. Verify Position is now closed and P&L calculated
        var openPositionsAfterSell = await _positionService.GetPositionsAsync(_testAccount.Id, openOnly: true);
        Assert.Empty(openPositionsAfterSell);

        var allPositions = await _positionService.GetPositionsAsync(_testAccount.Id, openOnly: false);
        Assert.Single(allPositions);
        var closedPos = allPositions[0];
        Assert.False(closedPos.IsOpen);
        Assert.Equal(0, closedPos.Quantity);
        Assert.Equal(400.00m, closedPos.RealizedGrossPnL);
        Assert.True(closedPos.RealizedNetPnL < 400.00m); // Net P&L after regulatory & brokerage charges
        Assert.True(closedPos.RealizedNetPnL > 350.00m);
        Assert.True(closedPos.TotalCharges > 0m);

        // 6. Portfolio summary
        var summary = await _positionService.GetPortfolioSummaryAsync(_testAccount.Id);
        Assert.Equal(0, summary.OpenPositionsCount);
        Assert.Equal(400.00m, summary.TotalRealizedGrossPnL);
        Assert.Equal(closedPos.RealizedNetPnL, summary.TotalRealizedNetPnL);
    }
}
