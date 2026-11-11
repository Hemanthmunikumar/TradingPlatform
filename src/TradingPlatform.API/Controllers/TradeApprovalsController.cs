using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Approvals;
using TradingPlatform.Contracts.Approvals;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/trade-approvals")]
public class TradeApprovalsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PendingTradeApprovalDto>> CreateApproval(
        [FromBody] CreateTradeApprovalRequest request,
        CancellationToken ct)
    {
        var approval = await mediator.Send(new CreateTradeApprovalCommand(request), ct);
        return CreatedAtAction(nameof(GetPendingApprovals), new { tradingAccountId = approval.TradingAccountId }, approval);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApprovalDecisionResultDto>> RejectApproval(
        Guid id,
        [FromBody] RejectApprovalRequest? request,
        CancellationToken ct)
    {
        Guid? userId = null;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var parsed))
        {
            userId = parsed;
        }

        var result = await mediator.Send(new RejectApprovalCommand(id, request?.Reason, userId), ct);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<PendingTradeApprovalDto>>> GetPendingApprovals(
        [FromQuery] Guid? tradingAccountId,
        CancellationToken ct)
    {
        var approvals = await mediator.Send(new GetPendingApprovalsQuery(tradingAccountId), ct);
        return Ok(approvals);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PendingTradeApprovalDto>>> GetApprovalHistory(
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var approvals = await mediator.Send(new GetApprovalHistoryQuery(tradingAccountId, page, pageSize), ct);
        return Ok(approvals);
    }

    [HttpPost("process-expired")]
    public async Task<ActionResult<int>> ProcessExpiredApprovals(CancellationToken ct)
    {
        var processed = await mediator.Send(new ProcessExpiredApprovalsCommand(), ct);
        return Ok(new { processedCount = processed });
    }
}
