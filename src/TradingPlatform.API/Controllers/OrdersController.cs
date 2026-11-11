using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Orders;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var order = await mediator.Send(new CreateOrderCommand(request), ct);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<OrderDto>> SubmitOrder(Guid id, CancellationToken ct)
    {
        var order = await mediator.Send(new SubmitOrderCommand(id), ct);
        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] string? status,
        [FromQuery] string? side,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new OrderFilterRequest(
            TradingAccountId: tradingAccountId,
            Symbol: symbol,
            Status: status,
            Side: side,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            PageNumber: page,
            PageSize: pageSize
        );

        var orders = await mediator.Send(new GetOrdersQuery(filter), ct);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken ct)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(id), ct);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> CancelOrder(Guid id, [FromQuery] string? reason, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelOrderCommand(new CancelOrderRequest(id, reason)), ct);
        if (!result) return BadRequest(new { message = "Order could not be cancelled." });
        return NoContent();
    }
}
