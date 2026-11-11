using MediatR;
using TradingPlatform.Application.Interfaces.Strategies;
using TradingPlatform.Contracts.Strategies;

namespace TradingPlatform.Application.Features.Strategies;

public record GetStrategiesQuery : IRequest<IReadOnlyList<StrategyDefinitionDto>>;

public class GetStrategiesHandler(IStrategyEngine strategyEngine) 
    : IRequestHandler<GetStrategiesQuery, IReadOnlyList<StrategyDefinitionDto>>
{
    public async Task<IReadOnlyList<StrategyDefinitionDto>> Handle(GetStrategiesQuery request, CancellationToken cancellationToken)
    {
        return await strategyEngine.GetStrategiesAsync(cancellationToken);
    }
}

public record CreateStrategyCommand(CreateStrategyRequest Request) : IRequest<StrategyDefinitionDto>;

public class CreateStrategyHandler(IStrategyEngine strategyEngine)
    : IRequestHandler<CreateStrategyCommand, StrategyDefinitionDto>
{
    public async Task<StrategyDefinitionDto> Handle(CreateStrategyCommand request, CancellationToken cancellationToken)
    {
        return await strategyEngine.CreateStrategyAsync(request.Request, cancellationToken);
    }
}

public record EvaluateStrategyCommand(Guid StrategyId, Guid? InstrumentId = null) : IRequest<IReadOnlyList<TradingSignalDto>>;

public class EvaluateStrategyHandler(IStrategyEngine strategyEngine)
    : IRequestHandler<EvaluateStrategyCommand, IReadOnlyList<TradingSignalDto>>
{
    public async Task<IReadOnlyList<TradingSignalDto>> Handle(EvaluateStrategyCommand request, CancellationToken cancellationToken)
    {
        return await strategyEngine.EvaluateStrategyAsync(request.StrategyId, request.InstrumentId, cancellationToken);
    }
}

public record GetSignalsQuery(Guid? StrategyId = null, int Limit = 50) : IRequest<IReadOnlyList<TradingSignalDto>>;

public class GetSignalsHandler(IStrategyEngine strategyEngine)
    : IRequestHandler<GetSignalsQuery, IReadOnlyList<TradingSignalDto>>
{
    public async Task<IReadOnlyList<TradingSignalDto>> Handle(GetSignalsQuery request, CancellationToken cancellationToken)
    {
        return await strategyEngine.GetSignalsAsync(request.StrategyId, request.Limit, cancellationToken);
    }
}

public record GetIndicatorsQuery(string Symbol, string Exchange = "NSE", string Timeframe = "5m") : IRequest<IndicatorValuesDto>;

public class GetIndicatorsHandler(IIndicatorService indicatorService)
    : IRequestHandler<GetIndicatorsQuery, IndicatorValuesDto>
{
    public async Task<IndicatorValuesDto> Handle(GetIndicatorsQuery request, CancellationToken cancellationToken)
    {
        return await indicatorService.GetIndicatorSnapshotAsync(request.Symbol, request.Exchange, request.Timeframe, cancellationToken);
    }
}
