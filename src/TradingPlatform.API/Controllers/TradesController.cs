using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Trades;
using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/trades")]
public class TradesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradeHistoryDto>>> GetTradeHistory(
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] string? side,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TradeHistoryFilterRequest(
            TradingAccountId: tradingAccountId,
            Symbol: symbol,
            Side: side,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            PageNumber: page,
            PageSize: pageSize
        );

        var trades = await mediator.Send(new GetTradeHistoryQuery(filter), ct);
        return Ok(trades);
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportTradeHistoryCsv(
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] string? side,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var filter = new TradeHistoryFilterRequest(
            TradingAccountId: tradingAccountId,
            Symbol: symbol,
            Side: side,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            PageNumber: 1,
            PageSize: 10000
        );

        var csvBytes = await mediator.Send(new ExportTradeHistoryCsvQuery(filter), ct);
        var filename = $"trades_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(csvBytes, "text/csv", filename);
    }
}
