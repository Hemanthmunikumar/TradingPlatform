using TradingPlatform.Contracts.Strategies;

namespace TradingPlatform.Application.Interfaces.Strategies;

public interface IStrategyEngine
{
    Task<IReadOnlyList<StrategyDefinitionDto>> GetStrategiesAsync(CancellationToken ct = default);
    Task<StrategyDefinitionDto> CreateStrategyAsync(CreateStrategyRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TradingSignalDto>> EvaluateStrategyAsync(Guid strategyDefinitionId, Guid? instrumentId = null, CancellationToken ct = default);
    Task<IReadOnlyList<TradingSignalDto>> GetSignalsAsync(Guid? strategyId = null, int limit = 50, CancellationToken ct = default);
    Task SeedDefaultStrategiesAsync(CancellationToken ct = default);
}

public interface IIndicatorService
{
    Task<IndicatorValuesDto> GetIndicatorSnapshotAsync(string symbol, string exchange = "NSE", string timeframe = "5m", CancellationToken ct = default);
}
