namespace TradingPlatform.Contracts.MarketData;

public record InstrumentDto(
    Guid Id,
    string Exchange,
    string TradingSymbol,
    string InstrumentToken,
    string? ISIN,
    string CompanyName,
    string InstrumentType,
    int LotSize,
    decimal TickSize,
    string Segment,
    bool IsActive
);

public record SearchInstrumentsRequest(
    string? Query = null,
    string? Exchange = "NSE",
    int Limit = 20
);

public record MarketCandleDto(
    DateTime TimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal? VWAP
);

public record GetCandlesRequest(
    Guid InstrumentId,
    string Timeframe = "5m",
    DateTime? FromUtc = null,
    DateTime? ToUtc = null
);

public record MarketQuoteDto(
    string TradingSymbol,
    string Exchange,
    decimal LastPrice,
    decimal Change,
    decimal ChangePercent,
    decimal Open,
    decimal High,
    decimal Low,
    decimal PreviousClose,
    long Volume,
    decimal BestBid,
    int BestBidQty,
    decimal BestAsk,
    int BestAskQty,
    DateTime LastUpdatedUtc
);

public record MarketStatusDto(
    bool IsMarketOpen,
    string CurrentSession, // "PreOpen", "Regular", "PostClose", "Closed"
    string Exchange,
    DateTime ServerTimeIst,
    TimeSpan TimeToNextSession
);

public record InstrumentUniverseDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<InstrumentDto> Instruments
);

public record CreateInstrumentUniverseRequest(
    string Name,
    string? Description,
    IReadOnlyList<Guid> InstrumentIds
);

public record AddInstrumentToUniverseRequest(
    Guid InstrumentId
);
