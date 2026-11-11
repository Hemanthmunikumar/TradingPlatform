using MediatR;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Features.Orders;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderDto>;

public class CreateOrderCommandHandler(IOrderService orderService) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        return orderService.CreateOrderAsync(request.Request, cancellationToken);
    }
}

public record SubmitOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public class SubmitOrderCommandHandler(IOrderService orderService) : IRequestHandler<SubmitOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        return orderService.SubmitOrderAsync(request.OrderId, cancellationToken);
    }
}

public record CancelOrderCommand(CancelOrderRequest Request) : IRequest<bool>;

public class CancelOrderCommandHandler(IOrderService orderService) : IRequestHandler<CancelOrderCommand, bool>
{
    public Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        return orderService.CancelOrderAsync(request.Request, cancellationToken);
    }
}
