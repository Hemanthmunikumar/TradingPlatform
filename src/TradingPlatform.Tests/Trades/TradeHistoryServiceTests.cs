using System.Text;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Contracts.Trades;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using TradingPlatform.Infrastructure.Trades;
using Xunit;

namespace TradingPlatform.Tests.Trades;

public class TradeHistoryServiceTests
{
    private readonly TradingDbContext _dbContext;
    private readonly TradeHistoryService _tradeHistoryService;
    private readonly Guid _testUserId;
    private readonly TradingAccount _testAccount;
    private readonly Instrument _testInstrument;

    public TradeHistoryServiceTests()
    {
        _testUserId = Guid.NewGuid();
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService { UserId = _testUserId, IsAdmin = true });
        _tradeHistoryService = new TradeHistoryService(_dbContext);

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
            TradingSymbol = "RELIANCE",
            CompanyName = "Reliance Industries Ltd",
            Exchange = "NSE",
            IsActive = true
        };

        _dbContext.BrokerProviders.Add(provider);
        _dbContext.BrokerConnections.Add(connection);
        _dbContext.TradingAccounts.Add(_testAccount);
        _dbContext.Instruments.Add(_testInstrument);

        // Seed sample trade executions
        var order1 = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Buy,
            ProductType = ProductType.MIS,
            Quantity = 20,
            Price = 2500m
        };

        var exec1 = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = order1.Id,
            TradingOrder = order1,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Instrument = _testInstrument,
            Side = OrderSide.Buy,
            Quantity = 20,
            Price = 2500m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var order2 = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Side = OrderSide.Sell,
            ProductType = ProductType.MIS,
            Quantity = 20,
            Price = 2550m
        };

        var exec2 = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TradingOrderId = order2.Id,
            TradingOrder = order2,
            TradingAccountId = _testAccount.Id,
            InstrumentId = _testInstrument.Id,
            Instrument = _testInstrument,
            Side = OrderSide.Sell,
            Quantity = 20,
            Price = 2550m,
            Exchange = "NSE",
            ExecutedAtUtc = DateTime.UtcNow.AddMinutes(-30)
        };

        _dbContext.TradingOrders.AddRange(order1, order2);
        _dbContext.TradeExecutions.AddRange(exec1, exec2);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetTradeHistory_WithFilters_ReturnsMatchingExecutions()
    {
        var filter = new TradeHistoryFilterRequest(
            TradingAccountId: _testAccount.Id,
            Symbol: "RELIANCE",
            Side: "Sell"
        );

        var result = await _tradeHistoryService.GetTradeHistoryAsync(filter);

        Assert.Single(result);
        var trade = result[0];
        Assert.Equal("RELIANCE", trade.Symbol);
        Assert.Equal("Sell", trade.Side);
        Assert.Equal(20, trade.Quantity);
        Assert.Equal(2550m, trade.Price);
    }

    [Fact]
    public async Task ExportTradeHistoryCsv_GeneratesValidCsvContent()
    {
        var filter = new TradeHistoryFilterRequest(
            TradingAccountId: _testAccount.Id
        );

        var csvBytes = await _tradeHistoryService.ExportTradeHistoryCsvAsync(filter);
        var csvString = Encoding.UTF8.GetString(csvBytes);

        Assert.NotEmpty(csvString);
        Assert.Contains("ExecutionId,TimestampIST,Symbol,CompanyName,Side,ProductType,Quantity,ExecutionPrice,TotalValue,EntryPrice,RealizedPnL,Exchange", csvString);
        Assert.Contains("RELIANCE", csvString);
        Assert.Contains("2500.00", csvString);
        Assert.Contains("2550.00", csvString);
    }
}
