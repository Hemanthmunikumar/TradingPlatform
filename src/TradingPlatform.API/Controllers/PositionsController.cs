using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Positions;
using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/positions")]
public class PositionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PositionDto>>> GetPositions(
        [FromQuery] Guid tradingAccountId,
        [FromQuery] bool openOnly = true,
        CancellationToken ct = default)
    {
        var positions = await mediator.Send(new GetPositionsQuery(tradingAccountId, openOnly), ct);
        return Ok(positions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PositionDto>> GetPositionById(Guid id, CancellationToken ct)
    {
        var position = await mediator.Send(new GetPositionByIdQuery(id), ct);
        if (position == null) return NotFound();
        return Ok(position);
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult> ClosePosition(Guid id, [FromBody] ClosePositionRequest? request, CancellationToken ct)
    {
        var reason = request?.Reason ?? "Manual Exit";
        var result = await mediator.Send(new ClosePositionCommand(new ClosePositionRequest(id, reason)), ct);
        if (!result) return BadRequest(new { message = "Position could not be closed." });
        return Ok(new { message = "Position close order placed." });
    }

    [HttpGet("/api/v1/portfolio/summary")]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolioSummary(
        [FromQuery] Guid tradingAccountId,
        CancellationToken ct)
    {
        var summary = await mediator.Send(new GetPortfolioSummaryQuery(tradingAccountId), ct);
        return Ok(summary);
    }
}
