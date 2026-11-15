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

public class AiAdvisorConfigDto
{
    public string Provider { get; set; } = "GeminiPro"; // "GeminiPro", "OpenAI", "Claude", "BuiltInQuant"
    public string? ApiKey { get; set; }
    public bool HasApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-pro";
    public decimal MinConfidenceThreshold { get; set; } = 0.80m;
    public bool EnableDipAccumulation { get; set; } = true;
    public bool EnableSpecialWatchlist { get; set; } = true;
    public string ActiveSource { get; set; } = "AppSettings"; // "UI" or "AppSettings"
}

public interface IAiMarketAdvisorService
{
    Task<AiAnalysisResult> AnalyzeStockAsync(AiAnalysisRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AiAnalysisResult>> ScanHoldingsAndWatchlistAsync(Guid? accountId = null, CancellationToken ct = default);
    Task<AiAdvisorConfigDto> GetConfigurationAsync(CancellationToken ct = default);
    Task<AiAdvisorConfigDto> UpdateConfigurationAsync(AiAdvisorConfigDto updateDto, CancellationToken ct = default);
    Task<AiAdvisorConfigDto> ResetConfigurationAsync(CancellationToken ct = default);
}
