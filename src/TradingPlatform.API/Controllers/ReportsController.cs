using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportsController(
    IDailyReportGeneratorService reportService,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    ILogger<ReportsController> logger) : ControllerBase
{
    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<ApiResponse<DailyTradingReportDto>>> GetDailyReport(
        [FromQuery] string? date,
        [FromQuery] Guid? tradingAccountId,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<DailyTradingReportDto>.Fail("Unauthorized"));
        }

        var reportDate = DateOnly.TryParse(date, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await reportService.GenerateDailyReportAsync(userId, reportDate, tradingAccountId, ct);
        return Ok(ApiResponse<DailyTradingReportDto>.Ok(report));
    }

    [HttpPost("daily/dispatch")]
    public async Task<ActionResult<ApiResponse<bool>>> DispatchDailyReport(
        [FromQuery] string? date,
        CancellationToken ct = default)
    {
        var reportDate = DateOnly.TryParse(date, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);
        await reportService.DispatchDailyReportsAsync(reportDate, ct);
        return Ok(ApiResponse<bool>.Ok(true, $"Dispatched daily digest for {reportDate:yyyy-MM-dd}"));
    }
}
