namespace TradingPlatform.Application.Interfaces.AI;

public record AiAnalysisRequest(
    string Symbol,
    string CompanyName,
    decimal CurrentPrice,
    decimal DayChangePercent,
    decimal Rsi14,
    decimal Ema20,
    decimal Ema50,
    decimal Ema200,
    decimal Vwap,
    decimal VolumeRatio,
    bool IsExistingHolding = false,
    decimal? AverageBuyPrice = null,
    int? ExistingQuantity = null,
    string MarketRegime = "Neutral"
);

public record AiAnalysisResult(
    string Symbol,
    string CompanyName,
    string Action, // STRONG_BUY, BUY_THE_DIP, HOLD, BOOK_PROFIT_50, EXIT_STOP_LOSS
    decimal ConvictionScore, // e.g. 0.88 (88%)
    decimal CurrentPrice,
    decimal SuggestedPrice,
    int SuggestedQuantity,
    decimal StopLoss,
    decimal Target1,
    decimal Target2,
    string Summary,
    string TechnicalRationale,
    string RiskNote,
    string ProviderName,
    DateTime AnalyzedAtUtc
);

public interface IAiMarketAdvisorService
{
    Task<AiAnalysisResult> AnalyzeStockAsync(AiAnalysisRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AiAnalysisResult>> ScanHoldingsAndWatchlistAsync(Guid? accountId = null, CancellationToken ct = default);
}
