using MediatR;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Features.Orders;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler(IOrderService orderService) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return orderService.GetOrderByIdAsync(request.OrderId, cancellationToken);
    }
}

public record GetOrdersQuery(OrderFilterRequest Filter) : IRequest<IReadOnlyList<OrderDto>>;

public class GetOrdersQueryHandler(IOrderService orderService) : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderDto>>
{
    public Task<IReadOnlyList<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return orderService.GetOrdersAsync(request.Filter, cancellationToken);
    }
}
