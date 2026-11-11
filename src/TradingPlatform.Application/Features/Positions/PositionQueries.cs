using MediatR;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Features.Positions;

public record GetPositionsQuery(Guid TradingAccountId, bool OpenOnly = true) : IRequest<IReadOnlyList<PositionDto>>;

public class GetPositionsQueryHandler(IPositionService positionService) : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionDto>>
{
    public Task<IReadOnlyList<PositionDto>> Handle(GetPositionsQuery request, CancellationToken cancellationToken)
    {
        return positionService.GetPositionsAsync(request.TradingAccountId, request.OpenOnly, cancellationToken);
    }
}

public record GetPortfolioSummaryQuery(Guid TradingAccountId) : IRequest<PortfolioSummaryDto>;

public class GetPortfolioSummaryQueryHandler(IPositionService positionService) : IRequestHandler<GetPortfolioSummaryQuery, PortfolioSummaryDto>
{
    public Task<PortfolioSummaryDto> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        return positionService.GetPortfolioSummaryAsync(request.TradingAccountId, cancellationToken);
    }
}

public record GetPositionByIdQuery(Guid PositionId) : IRequest<PositionDto?>;

public class GetPositionByIdQueryHandler(IPositionService positionService) : IRequestHandler<GetPositionByIdQuery, PositionDto?>
{
    public Task<PositionDto?> Handle(GetPositionByIdQuery request, CancellationToken cancellationToken)
    {
        return positionService.GetPositionByIdAsync(request.PositionId, cancellationToken);
    }
}
