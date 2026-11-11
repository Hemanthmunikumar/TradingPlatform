using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Strategies;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.Strategies;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/indicators")]
public class IndicatorsController(ISender sender) : ControllerBase
{
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(ApiResponse<IndicatorValuesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIndicators(
        string symbol, 
        [FromQuery] string exchange = "NSE", 
        [FromQuery] string timeframe = "5m", 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest(ApiResponse<object>.Fail("Symbol is required."));
        }

        var result = await sender.Send(new GetIndicatorsQuery(symbol, exchange, timeframe), cancellationToken);
        return Ok(ApiResponse<IndicatorValuesDto>.Ok(result));
    }
}
