using MediatR;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Contracts.Risk;

namespace TradingPlatform.Application.Features.Risk;

public record EvaluateOrderRiskCommand(OrderRiskEvaluationRequest Request) : IRequest<RiskEvaluationResultDto>;

public class EvaluateOrderRiskHandler(IRiskValidationService riskService)
    : IRequestHandler<EvaluateOrderRiskCommand, RiskEvaluationResultDto>
{
    public async Task<RiskEvaluationResultDto> Handle(EvaluateOrderRiskCommand request, CancellationToken cancellationToken)
    {
        return await riskService.ValidateOrderAsync(request.Request, cancellationToken);
    }
}

public record GetDailyRiskSummaryQuery(Guid TradingAccountId) : IRequest<DailyRiskSummaryDto>;

public class GetDailyRiskSummaryHandler(IRiskValidationService riskService)
    : IRequestHandler<GetDailyRiskSummaryQuery, DailyRiskSummaryDto>
{
    public async Task<DailyRiskSummaryDto> Handle(GetDailyRiskSummaryQuery request, CancellationToken cancellationToken)
    {
        return await riskService.GetDailyRiskSummaryAsync(request.TradingAccountId, cancellationToken);
    }
}

public record ToggleKillSwitchCommand(ToggleKillSwitchRequest Request) : IRequest<bool>;

public class ToggleKillSwitchHandler(IRiskValidationService riskService)
    : IRequestHandler<ToggleKillSwitchCommand, bool>
{
    public async Task<bool> Handle(ToggleKillSwitchCommand request, CancellationToken cancellationToken)
    {
        return await riskService.ToggleKillSwitchAsync(request.Request, cancellationToken);
    }
}
