using TradingPlatform.Contracts.Strategies;

namespace TradingPlatform.Application.Interfaces.Strategies;

public interface IBacktestingService
{
    Task<BacktestResultDto> RunBacktestAsync(BacktestRequestDto request, CancellationToken ct = default);
}
