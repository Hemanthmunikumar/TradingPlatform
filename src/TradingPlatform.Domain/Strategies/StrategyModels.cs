using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Domain.Indicators;

namespace TradingPlatform.Domain.Strategies;

public record StrategyContext(
    Instrument Instrument,
    TradingAccount Account,
    StrategyVersion Version,
    IReadOnlyList<OhlcvBar> Candles,
    decimal CurrentPrice,
    DateTime TimestampUtc
);

public record StrategyEvaluationResult(
    SignalType Action, // Buy, Sell, Hold
    decimal Price,
    int SuggestedQuantity,
    decimal? StopLoss,
    decimal? Target,
    string Reason,
    decimal Confidence,
    IndicatorSnapshot Indicators
);

public interface IStrategy
{
    string Code { get; }
    string Name { get; }
    StrategyType StrategyType { get; }
    StrategyEvaluationResult Evaluate(StrategyContext context);
}
