namespace TradingPlatform.Application.Interfaces.AI;

public record PreMarket21EmaSignalDto(
    string Symbol,
    string Name,
    decimal SpotPrice,
    decimal PrevClose,
    decimal GainPercent,
    long Volume,
    decimal Ema21,
    decimal DistanceToEmaPercent,
    string Status, // "ACTIVE_BUY_AT_21EMA" or "MONITORING_PULLBACK"
    string SuggestedAtmOption, // e.g. "1000 CE", "1250 CE"
    decimal StopLoss,
    decimal TargetPrice,
    string Strategy,
    string Timeframe,
    DateTime Timestamp,
    string OptionSymbol = "",
    decimal OptionLtp = 0m,
    decimal OptionEma21 = 0m,
    decimal OptionStopLoss = 0m,
    decimal OptionTarget = 0m,
    bool UptrendConfirmed = true,
    string NewsSentiment = "NEUTRAL",
    string NewsHeadline = ""
);

public record PreMarketScanResponse(
    string Strategy,
    decimal MinGain,
    decimal MaxGain,
    long MinVolume,
    int TotalScanned,
    int HitCount,
    IReadOnlyList<PreMarket21EmaSignalDto> Signals
);

public record IndiaOrbHitDto(
    string Symbol,
    string Exchange,
    IReadOnlyList<string> Setups,
    decimal Price,
    decimal PrevClose,
    decimal Rsi,
    long Volume,
    bool NearUpperCircuit,
    string SquareOff,
    string SessionClose
);

public record IndiaInsiderDealDto(
    string Id,
    string Kind,
    string Date,
    string Symbol,
    string Security,
    string Client,
    string Side,
    string Action,
    long Shares,
    decimal Price,
    decimal ValueInr,
    bool SameDayBothSides
);

public interface IIndiaDayTraderService
{
    Task<PreMarketScanResponse> ScanPreMarket21EmaAsync(
        decimal minGain = 1.0m,
        decimal maxGain = 2.5m,
        long minVolume = 20000,
        string? symbols = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<IndiaOrbHitDto>> ScanOrbBreakoutsAsync(
        string? symbols = null,
        int minPrice = 20,
        long minTurnover = 50000000,
        CancellationToken ct = default);

    Task<IReadOnlyList<IndiaInsiderDealDto>> GetInsiderDealsAsync(
        int limit = 20,
        string? symbol = null,
        CancellationToken ct = default);
}
