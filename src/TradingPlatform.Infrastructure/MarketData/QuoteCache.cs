using Microsoft.Extensions.Caching.Memory;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.Infrastructure.MarketData;

public class QuoteCache(IMemoryCache memoryCache) : IQuoteCache
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(2);

    private static string GetCacheKey(string symbol, string exchange) => 
        $"quote:{exchange.ToUpperInvariant()}:{symbol.ToUpperInvariant()}";

    public MarketQuoteDto? Get(string symbol, string exchange = "NSE")
    {
        return memoryCache.TryGetValue(GetCacheKey(symbol, exchange), out MarketQuoteDto? quote) ? quote : null;
    }

    public void Set(string symbol, string exchange, MarketQuoteDto quote, TimeSpan? ttl = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
        };

        memoryCache.Set(GetCacheKey(symbol, exchange), quote, options);
    }

    public void Invalidate(string symbol, string exchange = "NSE")
    {
        memoryCache.Remove(GetCacheKey(symbol, exchange));
    }
}
