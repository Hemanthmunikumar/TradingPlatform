using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Risk;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.Risk;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/risk")]
public class RiskController(ISender sender) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<RiskEvaluationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateOrder([FromBody] OrderRiskEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new EvaluateOrderRiskCommand(request), cancellationToken);
        return Ok(ApiResponse<RiskEvaluationResultDto>.Ok(
            result, 
            result.IsApproved ? "Order passed pre-order risk gate." : "Order blocked by pre-order risk gate."));
    }

    [HttpGet("accounts/{id:guid}/daily-summary")]
    [ProducesResponseType(typeof(ApiResponse<DailyRiskSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailySummary(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetDailyRiskSummaryQuery(id), cancellationToken);
        return Ok(ApiResponse<DailyRiskSummaryDto>.Ok(result));
    }

    [HttpPost("kill-switch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleKillSwitch([FromBody] ToggleKillSwitchRequest request, CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ToggleKillSwitchCommand(request), cancellationToken);
        string scope = request.TradingAccountId.HasValue ? $"Account {request.TradingAccountId}" : "System-Wide";
        return Ok(ApiResponse<bool>.Ok(
            result, 
            request.Activate ? $"{scope} Emergency Kill Switch ENGAGED." : $"{scope} Kill Switch DISARMED."));
    }
}
