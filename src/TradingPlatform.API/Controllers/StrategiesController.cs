using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Strategies;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.Strategies;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/strategies")]
public class StrategiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StrategyDefinitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStrategies(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetStrategiesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<StrategyDefinitionDto>>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StrategyDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateStrategy([FromBody] CreateStrategyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateStrategyCommand(request), cancellationToken);
        return Ok(ApiResponse<StrategyDefinitionDto>.Ok(result, "Strategy defined and initialized."));
    }

    [HttpPost("{id:guid}/evaluate")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TradingSignalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateStrategy(
        Guid id, 
        [FromQuery] Guid? instrumentId = null, 
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new EvaluateStrategyCommand(id, instrumentId), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TradingSignalDto>>.Ok(result, $"Strategy evaluated. Generated {result.Count} signals."));
    }

    [HttpGet("signals")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TradingSignalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSignals(
        [FromQuery] Guid? strategyId = null, 
        [FromQuery] int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetSignalsQuery(strategyId, limit), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TradingSignalDto>>.Ok(result));
    }
}
