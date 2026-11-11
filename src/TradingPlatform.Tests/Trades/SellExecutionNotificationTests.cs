using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Trades;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.Trades;

public class SellExecutionNotificationTests
{
    private readonly TradingDbContext _dbContext;
    private readonly Mock<ITradingNotificationService> _mockNotifications;
    private readonly PositionService _positionService;

    private readonly Guid _testUserId;
    private readonly TradingAccount _testAccount;
    private readonly Instrument _testInstrument;

    public SellExecutionNotificationTests()
    {
        _testUserId = Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService { UserId = _testUserId, IsAdmin = true });

        var calculator = new IndianBrokerageCalculator();
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

        _mockNotifications = new Mock<ITradingNotificationService>();
        var logger = new Mock<ILogger<PositionService>>();

        _positionService = new PositionService(
            _dbContext,
            calculator,
            mockMarketData.Object,
            _mockNotifications.Object,
            logger.Object
        );

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
            DisplayName = "Paper Conn",
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
    public async Task UpdatePositionOnExecution_OnSellExecution_EmitsSellNotificationWithChargesAndPnl()
    {
        // 1. Establish open BUY position: 10 shares @ ₹1,850
        var buyOrder = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 10,
            Price = 1850m
        };
        _dbContext.TradingOrders.Add(buyOrder);

        var buyExecution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = buyOrder.Id,
            TradingOrder = buyOrder,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            Quantity = 10,
            Price = 1850m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-15)
        };
        _dbContext.TradeExecutions.Add(buyExecution);
        await _dbContext.SaveChangesAsync();

        await _positionService.UpdatePositionOnExecutionAsync(buyExecution);

        // 2. Execute SELL order: 10 shares @ ₹1,890 (+₹40/share gain)
        var sellOrder = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Sell,
            ProductType = ProductType.MIS,
            Quantity = 10,
            Price = 1890m,
            StatusMessage = "StrategySignal Exit"
        };
        _dbContext.TradingOrders.Add(sellOrder);

        var sellExecution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = sellOrder.Id,
            TradingOrder = sellOrder,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Sell,
            Quantity = 10,
            Price = 1890m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow
        };
        _dbContext.TradeExecutions.Add(sellExecution);
        await _dbContext.SaveChangesAsync();

        // 3. Process execution
        await _positionService.UpdatePositionOnExecutionAsync(sellExecution);

        // 4. Verify PublishSellExecutionAsync was called with complete breakdown
        _mockNotifications.Verify(
            n => n.PublishSellExecutionAsync(It.Is<SellExecutionNotificationDto>(dto =>
                dto.Symbol == "INFY" &&
                dto.Quantity == 10 &&
                dto.EntryPrice == 1850m &&
                dto.ExitPrice == 1890m &&
                dto.GrossPnL == 400.00m &&
                dto.NetRealizedPnL < 400.00m && // Deducted Indian regulatory & brokerage charges
                dto.NetRealizedPnL > 350.00m &&
                dto.Charges.Brokerage > 0 &&
                dto.Charges.TotalCharges > 0 &&
                dto.ReturnPercent > 2.0m
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdatePositionOnExecution_WhenStopLossHit_EmitsStopLossHitNotification()
    {
        // 1. Establish open BUY position with Stop Loss @ ₹1,830
        var buyOrder = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 10,
            Price = 1850m,
            TriggerPrice = 1830m
        };
        _dbContext.TradingOrders.Add(buyOrder);

        var buyExecution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = buyOrder.Id,
            TradingOrder = buyOrder,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            Quantity = 10,
            Price = 1850m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        };
        _dbContext.TradeExecutions.Add(buyExecution);
        await _dbContext.SaveChangesAsync();

        var pos = await _positionService.UpdatePositionOnExecutionAsync(buyExecution);
        Assert.NotNull(pos);
        pos.StopLoss = 1830m;
        await _dbContext.SaveChangesAsync();

        // 2. Execute SELL order at ₹1,825 (Stop Loss breached)
        var sellOrder = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Sell,
            ProductType = ProductType.MIS,
            Quantity = 10,
            Price = 1825m,
            StatusMessage = "StopLoss Triggered"
        };
        _dbContext.TradingOrders.Add(sellOrder);

        var sellExecution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = sellOrder.Id,
            TradingOrder = sellOrder,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Sell,
            Quantity = 10,
            Price = 1825m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow
        };
        _dbContext.TradeExecutions.Add(sellExecution);
        await _dbContext.SaveChangesAsync();

        await _positionService.UpdatePositionOnExecutionAsync(sellExecution);

        // 3. Verify PublishStopLossHitAsync was called with negative P&L
        _mockNotifications.Verify(
            n => n.PublishStopLossHitAsync(It.Is<SellExecutionNotificationDto>(dto =>
                dto.Symbol == "INFY" &&
                dto.ExitTrigger == "StopLoss" &&
                dto.GrossPnL < 0 &&
                dto.NetRealizedPnL < dto.GrossPnL
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
