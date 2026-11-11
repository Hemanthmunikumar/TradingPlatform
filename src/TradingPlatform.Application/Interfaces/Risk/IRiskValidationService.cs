using TradingPlatform.Contracts.Risk;

namespace TradingPlatform.Application.Interfaces.Risk;

public interface IRiskValidationService
{
    Task<RiskEvaluationResultDto> ValidateOrderAsync(OrderRiskEvaluationRequest request, CancellationToken ct = default);
    Task<DailyRiskSummaryDto> GetDailyRiskSummaryAsync(Guid tradingAccountId, CancellationToken ct = default);
    Task<bool> ToggleKillSwitchAsync(ToggleKillSwitchRequest request, CancellationToken ct = default);
}
