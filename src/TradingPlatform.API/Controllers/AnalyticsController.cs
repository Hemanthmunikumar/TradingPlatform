using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Interfaces.Analytics;
using TradingPlatform.Contracts.Analytics;
using TradingPlatform.Contracts.Common;

namespace TradingPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnalyticsController(
    IAnalyticsService analyticsService,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet("portfolio")]
    public async Task<ActionResult<ApiResponse<PortfolioAnalyticsDto>>> GetPortfolioAnalytics(
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<PortfolioAnalyticsDto>.Fail("Unauthorized"));
        }

        var result = await analyticsService.GetPortfolioAnalyticsAsync(userId, tradingAccountId, days, ct);
        return Ok(ApiResponse<PortfolioAnalyticsDto>.Ok(result));
    }
}
