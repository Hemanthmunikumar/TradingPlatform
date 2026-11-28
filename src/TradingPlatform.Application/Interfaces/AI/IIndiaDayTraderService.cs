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
    IReadOnlyList<PreMarket21EmaSignalDto> Signals,
    decimal StopLossPercent = 2.5m,
    decimal TargetPercent = 10.0m
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

public record IndiaNewsItemDto(
    string Source,
    string Kind,
    string Symbol,
    string Title,
    string Publisher,
    string Link,
    string TimeIst,
    IReadOnlyList<string> Tags,
    bool IsRedFlag
);

public record IndiaNewsResponse(
    string Symbol,
    decimal? Price,
    decimal? ChangePercent,
    bool HasRedFlag,
    string RedFlagReason,
    IReadOnlyList<IndiaNewsItemDto> Items
);

public record IndiaQuarterlyResultDto(
    string Fiscal,
    string EndDate,
    decimal? RevenueCrore,
    decimal? NetProfitCrore,
    decimal? RevenueYoyPercent,
    decimal? NetProfitYoyPercent,
    decimal? EbitdaMarginPercent,
    decimal? NetMarginPercent,
    decimal? EpsDiluted
);

public record IndiaAnalystResultDto(
    string Symbol,
    string NextResultsNote,
    bool ResultsImminent,
    IReadOnlyList<string> Changes,
    IReadOnlyList<IndiaQuarterlyResultDto> Quarters
);

public record IndiaEntryCheckDto(
    string Symbol,
    string Status,
    string Phase,
    string Reason,
    decimal? Level,
    decimal? LastClose,
    decimal? VolumeRatio,
    string SquareOffTime,
    string SessionCloseTime
);

public record IndiaTimeMachineDto(
    string Symbol,
    string Date,
    decimal Entry,
    decimal Stop,
    decimal Target,
    int Replays,
    decimal TargetFirstPercent,
    decimal StopFirstPercent,
    decimal NeitherPercent,
    decimal GreenPercent,
    decimal AvgRGross,
    decimal AvgRNet,
    decimal ChargesRoundTrip,
    string Verdict
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

    Task<IndiaNewsResponse> GetStockNewsAsync(
        string symbol,
        int hours = 48,
        CancellationToken ct = default);

    Task<IndiaAnalystResultDto> GetQuarterlyResultsAsync(
        string symbol,
        int quarters = 2,
        CancellationToken ct = default);

    Task<IndiaEntryCheckDto> CheckEntryConfirmationAsync(
        string symbol,
        decimal? level = null,
        decimal minVolumeX = 1.2m,
        CancellationToken ct = default);

    Task<IndiaTimeMachineDto> RunTimeMachineAsync(
        string symbol,
        decimal entry,
        decimal stop,
        decimal target,
        string? entryTime = null,
        CancellationToken ct = default);
}
