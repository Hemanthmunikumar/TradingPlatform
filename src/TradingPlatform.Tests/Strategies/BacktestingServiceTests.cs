using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Contracts.Strategies;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using TradingPlatform.Infrastructure.Strategies;
using Xunit;

namespace TradingPlatform.Tests.Strategies;

public class BacktestingServiceTests
{
    private TradingDbContext CreateDbContext(Guid? userId = null)
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"BacktestingTest_{Guid.NewGuid()}")
            .Options;

        return new TradingDbContext(options, new DefaultCurrentUserService { UserId = userId, IsAdmin = true });
    }

    [Fact]
    public async Task RunBacktest_ReplaysHistoricalCandlesAndComputesReturns()
    {
        using var db = CreateDbContext();
        var mockMarketData = new Mock<IMarketDataService>();

        // Generate synthetic trending series with 40 candles
        var candles = new List<MarketCandleDto>();
        var baseTime = DateTime.UtcNow.AddDays(-5);
        decimal price = 1000m;

        for (int i = 0; i < 40; i++)
        {
            // First 20 sideways, next 10 surge upwards, next 10 consolidate
            var delta = i < 20 ? 0.5m : i < 30 ? 5.0m : -1.0m;
            price += delta;

            candles.Add(new MarketCandleDto(
                TimestampUtc: baseTime.AddMinutes(i * 5),
                Open: price - 1m,
                High: price + 2m,
                Low: price - 2m,
                Close: price,
                Volume: 10000 + (i * 100),
                VWAP: price
            ));
        }

        var instrument = new Instrument
        {
            Id = Guid.NewGuid(),
            TradingSymbol = "TATASTEEL",
            CompanyName = "Tata Steel Limited",
            Exchange = "NSE",
            Segment = "NSE_EQ",
            LotSize = 1,
            TickSize = 0.05m,
            IsActive = true
        };
        db.Instruments.Add(instrument);
        await db.SaveChangesAsync();

        mockMarketData.Setup(m => m.GetHistoricalCandlesAsync(It.IsAny<GetCandlesRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candles);

        var chargesCalculator = new IndianBrokerageCalculator();
        var mockLogger = new Mock<ILogger<BacktestingService>>();

        var backtestService = new BacktestingService(db, mockMarketData.Object, chargesCalculator, mockLogger.Object);

        var req = new BacktestRequestDto(
            StrategyDefinitionId: Guid.NewGuid(),
            Symbol: "TATASTEEL",
            Exchange: "NSE",
            FromUtc: baseTime,
            ToUtc: baseTime.AddDays(1),
            InitialCapital: 100000m,
            Timeframe: "5m"
        );

        var result = await backtestService.RunBacktestAsync(req);

        result.Should().NotBeNull();
        result.Symbol.Should().Be("TATASTEEL");
        result.InitialCapital.Should().Be(100000m);
        result.TotalTrades.Should().BeGreaterThan(0);
        result.Trades.Should().NotBeEmpty();
        result.Trades[0].TotalCharges.Should().BeGreaterThan(0); // Accurate Indian brokerage & taxes deducted
    }
}
