using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.MarketData;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/instruments")]
public class InstrumentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstrumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? exchange = "NSE", [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var request = new SearchInstrumentsRequest(query, exchange, limit);
        var result = await sender.Send(new SearchInstrumentsQuery(request), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InstrumentDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InstrumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInstrumentByIdQuery(id), cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<object>.Fail("Instrument not found."));
        }
        return Ok(ApiResponse<InstrumentDto>.Ok(result));
    }

    [HttpGet("{id:guid}/candles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MarketCandleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandles(
        Guid id, 
        [FromQuery] string timeframe = "5m", 
        [FromQuery] DateTime? fromUtc = null, 
        [FromQuery] DateTime? toUtc = null, 
        CancellationToken cancellationToken = default)
    {
        var request = new GetCandlesRequest(id, timeframe, fromUtc, toUtc);
        var result = await sender.Send(new GetCandlesQuery(request), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MarketCandleDto>>.Ok(result));
    }

    [HttpGet("universes")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstrumentUniverseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUniverses(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetInstrumentUniversesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<InstrumentUniverseDto>>.Ok(result));
    }

    [HttpPost("universes")]
    [ProducesResponseType(typeof(ApiResponse<InstrumentUniverseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateUniverse([FromBody] CreateInstrumentUniverseRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new CreateInstrumentUniverseCommand(request), cancellationToken);
        return Ok(ApiResponse<InstrumentUniverseDto>.Ok(result, "Universe created successfully."));
    }
}
