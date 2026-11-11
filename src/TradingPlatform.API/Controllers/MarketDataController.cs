using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.MarketData;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/market-data")]
public class MarketDataController(ISender sender) : ControllerBase
{
    [HttpGet("quote")]
    [ProducesResponseType(typeof(ApiResponse<MarketQuoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuote([FromQuery] string symbol, [FromQuery] string exchange = "NSE", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest(ApiResponse<object>.Fail("Trading symbol is required."));
        }

        var result = await sender.Send(new GetMarketQuoteQuery(symbol, exchange), cancellationToken);
        return Ok(ApiResponse<MarketQuoteDto>.Ok(result));
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<MarketStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus([FromQuery] string exchange = "NSE", CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetMarketStatusQuery(exchange), cancellationToken);
        return Ok(ApiResponse<MarketStatusDto>.Ok(result));
    }
}
