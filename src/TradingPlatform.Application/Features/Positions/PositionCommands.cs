using MediatR;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Features.Positions;

public record ClosePositionCommand(ClosePositionRequest Request) : IRequest<bool>;

public class ClosePositionCommandHandler(
    IPositionService positionService,
    IOrderService orderService) : IRequestHandler<ClosePositionCommand, bool>
{
    public async Task<bool> Handle(ClosePositionCommand command, CancellationToken cancellationToken)
    {
        var position = await positionService.GetPositionByIdAsync(command.Request.PositionId, cancellationToken);
        if (position == null || !position.IsOpen || position.Quantity <= 0)
        {
            return false;
        }

        // Place opposite market order to close position
        var oppositeSide = position.Side.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? "Sell" : "Buy";

        var orderRequest = new CreateOrderRequest(
            TradingAccountId: position.TradingAccountId,
            InstrumentId: position.InstrumentId,
            Symbol: position.Symbol,
            Side: oppositeSide,
            OrderType: "Market",
            ProductType: position.ProductType,
            Quantity: position.Quantity,
            Price: position.CurrentPrice,
            StopLoss: null,
            Target: null
        );

        var order = await orderService.CreateOrderAsync(orderRequest, cancellationToken);
        var submittedOrder = await orderService.SubmitOrderAsync(order.Id, cancellationToken);

        return submittedOrder.Status is "Filled" or "Submitted" or "Accepted" or "Open";
    }
}
