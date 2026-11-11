using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.Infrastructure.MarketData;

public class MarketDataService(
    ITradingDbContext dbContext,
    IQuoteCache quoteCache,
    IInstrumentService instrumentService) : IMarketDataService
{
    private static readonly TimeZoneInfo IstTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    private static readonly Dictionary<string, decimal> BaseStockPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RELIANCE"] = 2950.0m,
        ["TCS"] = 4120.0m,
        ["HDFCBANK"] = 1540.0m,
        ["INFY"] = 1850.0m,
        ["ICICIBANK"] = 1210.0m,
        ["BHARTIARTL"] = 1620.0m,
        ["SBIN"] = 830.0m,
        ["ITC"] = 485.0m,
        ["LT"] = 3650.0m,
        ["HINDUNILVR"] = 2480.0m,
        ["KOTAKBANK"] = 1780.0m,
        ["AXISBANK"] = 1190.0m,
        ["BAJFINANCE"] = 6950.0m,
        ["MARUTI"] = 11200.0m,
        ["TATAMOTORS"] = 980.0m,
        ["SUNPHARMA"] = 1830.0m,
        ["TITAN"] = 3420.0m,
        ["WIPRO"] = 560.0m,
        ["ADANIENT"] = 2890.0m,
        ["NTPC"] = 395.0m
    };

    public MarketStatusDto GetMarketStatus(string exchange = "NSE", DateTime? referenceTimeUtc = null)
    {
        var utcNow = referenceTimeUtc ?? DateTime.UtcNow;
        var istTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, IstTimeZone);
        var dayOfWeek = istTime.DayOfWeek;
        var timeOfDay = istTime.TimeOfDay;

        var isWeekend = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        var preOpenStart = new TimeSpan(9, 0, 0);
        var regularStart = new TimeSpan(9, 15, 0);
        var regularEnd = new TimeSpan(15, 30, 0);
        var postCloseEnd = new TimeSpan(16, 0, 0);

        bool isOpen = false;
        string session;
        TimeSpan timeToNext;

        if (isWeekend)
        {
            session = "Closed";
            // Next open is Monday 09:15
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)dayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            var nextMondayOpen = istTime.Date.AddDays(daysUntilMonday).Add(regularStart);
            timeToNext = nextMondayOpen - istTime;
        }
        else if (timeOfDay < preOpenStart)
        {
            session = "Closed";
            timeToNext = preOpenStart - timeOfDay;
        }
        else if (timeOfDay < regularStart)
        {
            session = "PreOpen";
            isOpen = false;
            timeToNext = regularStart - timeOfDay;
        }
        else if (timeOfDay <= regularEnd)
        {
            session = "Regular";
            isOpen = true;
            timeToNext = regularEnd - timeOfDay;
        }
        else if (timeOfDay <= postCloseEnd)
        {
            session = "PostClose";
            isOpen = false;
            timeToNext = postCloseEnd - timeOfDay;
        }
        else
        {
            session = "Closed";
            var nextOpen = istTime.Date.AddDays(dayOfWeek == DayOfWeek.Friday ? 3 : 1).Add(regularStart);
            timeToNext = nextOpen - istTime;
        }

        return new MarketStatusDto(
            IsMarketOpen: isOpen,
            CurrentSession: session,
            Exchange: exchange.ToUpperInvariant(),
            ServerTimeIst: istTime,
            TimeToNextSession: timeToNext
        );
    }

    public async Task<MarketQuoteDto> GetQuoteAsync(string symbol, string exchange = "NSE", CancellationToken ct = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var ex = exchange.Trim().ToUpperInvariant();

        // 1. Check Quote Cache
        var cached = quoteCache.Get(sym, ex);
        if (cached != null)
        {
            return cached;
        }

        // 2. Fetch or seed instrument
        var instrument = await instrumentService.GetByTradingSymbolAsync(sym, ex, ct);
        var basePrice = BaseStockPrices.TryGetValue(sym, out var price) ? price : 1000.0m;

        // Deterministic pseudo-random variation based on current minute and symbol hash
        var randomFactor = (decimal)(RandomNumberGenerator.GetInt32(-150, 150)) / 10000m; // -1.5% to +1.5%
        var lastPrice = Math.Round(basePrice * (1 + randomFactor), 2);
        var previousClose = basePrice;
        var change = lastPrice - previousClose;
        var changePercent = Math.Round((change / previousClose) * 100m, 2);

        var tickSize = instrument?.TickSize ?? 0.05m;
        var bestBid = Math.Round(lastPrice - tickSize, 2);
        var bestAsk = Math.Round(lastPrice + tickSize, 2);

        var quote = new MarketQuoteDto(
            TradingSymbol: sym,
            Exchange: ex,
            LastPrice: lastPrice,
            Change: change,
            ChangePercent: changePercent,
            Open: Math.Round(basePrice * 1.001m, 2),
            High: Math.Round(Math.Max(lastPrice, basePrice * 1.015m), 2),
            Low: Math.Round(Math.Min(lastPrice, basePrice * 0.985m), 2),
            PreviousClose: previousClose,
            Volume: 1250000 + RandomNumberGenerator.GetInt32(1000, 50000),
            BestBid: bestBid,
            BestBidQty: RandomNumberGenerator.GetInt32(100, 1500),
            BestAsk: bestAsk,
            BestAskQty: RandomNumberGenerator.GetInt32(100, 1500),
            LastUpdatedUtc: DateTime.UtcNow
        );

        // 3. Cache quote with TTL
        quoteCache.Set(sym, ex, quote, TimeSpan.FromSeconds(2));

        return quote;
    }

    public async Task<IReadOnlyList<MarketCandleDto>> GetHistoricalCandlesAsync(GetCandlesRequest request, CancellationToken ct = default)
    {
        var instrument = await instrumentService.GetInstrumentByIdAsync(request.InstrumentId, ct);
        var sym = instrument?.TradingSymbol ?? "INFY";
        var basePrice = BaseStockPrices.TryGetValue(sym, out var price) ? price : 1000.0m;

        var timeframeMinutes = request.Timeframe.ToLowerInvariant() switch
        {
            "1m" => 1,
            "5m" => 5,
            "15m" => 15,
            "1h" => 60,
            "1d" => 375, // 6h 15m trading day
            _ => 5
        };

        var end = request.ToUtc ?? DateTime.UtcNow;
        var start = request.FromUtc ?? end.AddHours(-6);
        if (start >= end) start = end.AddHours(-6);

        // First check if DB has stored candles
        var dbCandles = await dbContext.MarketCandles
            .AsNoTracking()
            .Where(c => c.InstrumentId == request.InstrumentId &&
                        c.Timeframe == request.Timeframe &&
                        c.TimestampUtc >= start &&
                        c.TimestampUtc <= end)
            .OrderBy(c => c.TimestampUtc)
            .Select(c => new MarketCandleDto(
                c.TimestampUtc,
                c.Open,
                c.High,
                c.Low,
                c.Close,
                c.Volume,
                c.VWAP
            ))
            .ToListAsync(ct);

        if (dbCandles.Count > 0)
        {
            return dbCandles;
        }

        // Generate synthetic OHLCV candles
        var candles = new List<MarketCandleDto>();
        var current = start;
        var currentPrice = basePrice;

        while (current < end)
        {
            var drift = (decimal)(RandomNumberGenerator.GetInt32(-30, 35)) / 1000m; // small drift
            var open = currentPrice;
            var close = Math.Round(open * (1 + drift), 2);
            var high = Math.Round(Math.Max(open, close) + Math.Abs(open * 0.003m), 2);
            var low = Math.Round(Math.Min(open, close) - Math.Abs(open * 0.003m), 2);
            var volume = (long)RandomNumberGenerator.GetInt32(5000, 50000);
            var vwap = Math.Round((open + high + low + close) / 4m, 2);

            candles.Add(new MarketCandleDto(
                TimestampUtc: current,
                Open: open,
                High: high,
                Low: low,
                Close: close,
                Volume: volume,
                VWAP: vwap
            ));

            currentPrice = close;
            current = current.AddMinutes(timeframeMinutes);
        }

        return candles;
    }
}
