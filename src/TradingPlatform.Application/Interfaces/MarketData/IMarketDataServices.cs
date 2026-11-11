using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.Application.Interfaces.MarketData;

public interface IInstrumentService
{
    Task<IReadOnlyList<InstrumentDto>> SearchInstrumentsAsync(SearchInstrumentsRequest request, CancellationToken ct = default);
    Task<InstrumentDto?> GetInstrumentByIdAsync(Guid id, CancellationToken ct = default);
    Task<InstrumentDto?> GetByTradingSymbolAsync(string symbol, string exchange = "NSE", CancellationToken ct = default);
    Task<IReadOnlyList<InstrumentUniverseDto>> GetUniversesAsync(CancellationToken ct = default);
    Task<InstrumentUniverseDto> CreateUniverseAsync(CreateInstrumentUniverseRequest request, CancellationToken ct = default);
    Task<bool> AddInstrumentToUniverseAsync(Guid universeId, Guid instrumentId, CancellationToken ct = default);
    Task SeedDefaultInstrumentsAsync(CancellationToken ct = default);
}

public interface IMarketDataService
{
    Task<MarketQuoteDto> GetQuoteAsync(string symbol, string exchange = "NSE", CancellationToken ct = default);
    Task<IReadOnlyList<MarketCandleDto>> GetHistoricalCandlesAsync(GetCandlesRequest request, CancellationToken ct = default);
    MarketStatusDto GetMarketStatus(string exchange = "NSE", DateTime? referenceTimeUtc = null);
}

public interface IQuoteCache
{
    MarketQuoteDto? Get(string symbol, string exchange = "NSE");
    void Set(string symbol, string exchange, MarketQuoteDto quote, TimeSpan? ttl = null);
    void Invalidate(string symbol, string exchange = "NSE");
}
