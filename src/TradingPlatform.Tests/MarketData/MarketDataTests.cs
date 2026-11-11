using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Infrastructure.MarketData;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;
using Xunit;

namespace TradingPlatform.Tests.MarketData;

public class MarketStatusTests
{
    private readonly MarketDataService _marketDataService;

    public MarketStatusTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TradingDbContext(dbOptions, new DefaultCurrentUserService());
        var cache = new QuoteCache(new MemoryCache(new MemoryCacheOptions()));
        var mockInstrumentService = new Mock<IInstrumentService>();

        _marketDataService = new MarketDataService(db, cache, mockInstrumentService.Object);
    }

    [Fact]
    public void MarketStatus_DuringRegularSession_ReturnsOpen()
    {
        // 2026-11-11 (Wednesday) 10:30:00 IST = 05:00:00 UTC
        var utcWednesdayMorning = new DateTime(2026, 11, 11, 5, 0, 0, DateTimeKind.Utc);

        var status = _marketDataService.GetMarketStatus("NSE", utcWednesdayMorning);

        Assert.True(status.IsMarketOpen);
        Assert.Equal("Regular", status.CurrentSession);
        Assert.Equal("NSE", status.Exchange);
    }

    [Fact]
    public void MarketStatus_DuringPreOpen_ReturnsPreOpenAndNotOpen()
    {
        // 2026-11-11 (Wednesday) 09:05:00 IST = 03:35:00 UTC
        var utcPreOpen = new DateTime(2026, 11, 11, 3, 35, 0, DateTimeKind.Utc);

        var status = _marketDataService.GetMarketStatus("NSE", utcPreOpen);

        Assert.False(status.IsMarketOpen);
        Assert.Equal("PreOpen", status.CurrentSession);
    }

    [Fact]
    public void MarketStatus_DuringPostClose_ReturnsPostCloseAndNotOpen()
    {
        // 2026-11-11 (Wednesday) 15:45:00 IST = 10:15:00 UTC
        var utcPostClose = new DateTime(2026, 11, 11, 10, 15, 0, DateTimeKind.Utc);

        var status = _marketDataService.GetMarketStatus("NSE", utcPostClose);

        Assert.False(status.IsMarketOpen);
        Assert.Equal("PostClose", status.CurrentSession);
    }

    [Fact]
    public void MarketStatus_OnWeekend_ReturnsClosed()
    {
        // 2026-11-14 (Saturday) 11:00:00 IST = 05:30:00 UTC
        var utcSaturday = new DateTime(2026, 11, 14, 5, 30, 0, DateTimeKind.Utc);

        var status = _marketDataService.GetMarketStatus("NSE", utcSaturday);

        Assert.False(status.IsMarketOpen);
        Assert.Equal("Closed", status.CurrentSession);
    }
}

public class QuoteCacheTests
{
    private readonly QuoteCache _quoteCache;

    public QuoteCacheTests()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _quoteCache = new QuoteCache(memoryCache);
    }

    [Fact]
    public void SetAndGet_ReturnsCachedQuote()
    {
        var quote = new MarketQuoteDto(
            TradingSymbol: "INFY",
            Exchange: "NSE",
            LastPrice: 1850.50m,
            Change: 15.50m,
            ChangePercent: 0.84m,
            Open: 1840.00m,
            High: 1860.00m,
            Low: 1835.00m,
            PreviousClose: 1835.00m,
            Volume: 1500000,
            BestBid: 1850.45m,
            BestBidQty: 500,
            BestAsk: 1850.55m,
            BestAskQty: 500,
            LastUpdatedUtc: DateTime.UtcNow
        );

        _quoteCache.Set("INFY", "NSE", quote);
        var retrieved = _quoteCache.Get("INFY", "NSE");

        Assert.NotNull(retrieved);
        Assert.Equal("INFY", retrieved.TradingSymbol);
        Assert.Equal(1850.50m, retrieved.LastPrice);
    }

    [Fact]
    public void Invalidate_RemovesQuoteFromCache()
    {
        var quote = new MarketQuoteDto(
            TradingSymbol: "TCS",
            Exchange: "NSE",
            LastPrice: 4120.00m,
            Change: 20.00m,
            ChangePercent: 0.49m,
            Open: 4110.00m,
            High: 4130.00m,
            Low: 4100.00m,
            PreviousClose: 4100.00m,
            Volume: 800000,
            BestBid: 4119.95m,
            BestBidQty: 200,
            BestAsk: 4120.05m,
            BestAskQty: 200,
            LastUpdatedUtc: DateTime.UtcNow
        );

        _quoteCache.Set("TCS", "NSE", quote);
        Assert.NotNull(_quoteCache.Get("TCS", "NSE"));

        _quoteCache.Invalidate("TCS", "NSE");
        Assert.Null(_quoteCache.Get("TCS", "NSE"));
    }
}

public class CandleAndQuoteIntegrityTests
{
    private readonly MarketDataService _marketDataService;
    private readonly TradingDbContext _dbContext;

    public CandleAndQuoteIntegrityTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TradingDbContext(dbOptions, new DefaultCurrentUserService());
        var cache = new QuoteCache(new MemoryCache(new MemoryCacheOptions()));
        var mockInstrumentService = new Mock<IInstrumentService>();

        mockInstrumentService
            .Setup(s => s.GetByTradingSymbolAsync("INFY", "NSE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstrumentDto(
                Id: Guid.NewGuid(),
                Exchange: "NSE",
                TradingSymbol: "INFY",
                InstrumentToken: "408065",
                ISIN: "INE009A01021",
                CompanyName: "Infosys Limited",
                InstrumentType: "EQUITY",
                LotSize: 1,
                TickSize: 0.05m,
                Segment: "NSE_EQ",
                IsActive: true
            ));

        mockInstrumentService
            .Setup(s => s.GetInstrumentByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstrumentDto(
                Id: Guid.NewGuid(),
                Exchange: "NSE",
                TradingSymbol: "INFY",
                InstrumentToken: "408065",
                ISIN: "INE009A01021",
                CompanyName: "Infosys Limited",
                InstrumentType: "EQUITY",
                LotSize: 1,
                TickSize: 0.05m,
                Segment: "NSE_EQ",
                IsActive: true
            ));

        _marketDataService = new MarketDataService(_dbContext, cache, mockInstrumentService.Object);
    }

    [Fact]
    public async Task GetQuoteAsync_MaintainsBidAskSpreadIntegrity()
    {
        var quote = await _marketDataService.GetQuoteAsync("INFY", "NSE");

        Assert.NotNull(quote);
        Assert.True(quote.LastPrice > 0, "Last price must be positive");
        Assert.True(quote.BestBid <= quote.LastPrice, "Bid price should be <= Last price");
        Assert.True(quote.BestAsk >= quote.LastPrice, "Ask price should be >= Last price");
        Assert.True(quote.BestBid < quote.BestAsk, "Bid must be strictly less than Ask");
    }

    [Fact]
    public async Task GetHistoricalCandlesAsync_MaintainsOhclIntegrity()
    {
        var request = new GetCandlesRequest(
            InstrumentId: Guid.NewGuid(),
            Timeframe: "5m",
            FromUtc: DateTime.UtcNow.AddHours(-2),
            ToUtc: DateTime.UtcNow
        );

        var candles = await _marketDataService.GetHistoricalCandlesAsync(request);

        Assert.NotEmpty(candles);
        foreach (var c in candles)
        {
            Assert.True(c.High >= c.Open, $"High ({c.High}) must be >= Open ({c.Open})");
            Assert.True(c.High >= c.Close, $"High ({c.High}) must be >= Close ({c.Close})");
            Assert.True(c.Low <= c.Open, $"Low ({c.Low}) must be <= Open ({c.Open})");
            Assert.True(c.Low <= c.Close, $"Low ({c.Low}) must be <= Close ({c.Close})");
            Assert.True(c.High >= c.Low, $"High ({c.High}) must be >= Low ({c.Low})");
            Assert.True(c.Volume > 0, "Volume must be positive");
        }
    }
}
