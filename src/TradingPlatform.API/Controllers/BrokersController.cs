using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Brokers;
using TradingPlatform.Contracts.Brokers;
using TradingPlatform.Contracts.Common;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/brokers")]
public class BrokersController(ISender sender) : ControllerBase
{
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BrokerProviderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrokerProvidersQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BrokerProviderDto>>.Ok(result));
    }

    [HttpGet("connections")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BrokerConnectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnections(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBrokerConnectionsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BrokerConnectionDto>>.Ok(result));
    }

    [HttpPost("connections")]
    [ProducesResponseType(typeof(ApiResponse<BrokerConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateConnection([FromBody] CreateBrokerConnectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new CreateBrokerConnectionCommand(request), cancellationToken);
            return Ok(ApiResponse<BrokerConnectionDto>.Ok(result, "Broker connection established."));
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("connections/{id:guid}/auth-url")]
    [ProducesResponseType(typeof(ApiResponse<AuthUrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuthUrl(Guid id, [FromBody] GetAuthUrlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new GetBrokerAuthUrlQuery(id, request.RedirectUri, request.State), cancellationToken);
            return Ok(ApiResponse<AuthUrlResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("connections/{id:guid}/exchange-token")]
    [ProducesResponseType(typeof(ApiResponse<BrokerConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExchangeToken(Guid id, [FromBody] ExchangeTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new ExchangeBrokerTokenCommand(id, request), cancellationToken);
            return Ok(ApiResponse<BrokerConnectionDto>.Ok(result, "Broker authentication successful."));
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("connections/{id:guid}/toggle-mode")]
    [ProducesResponseType(typeof(ApiResponse<BrokerConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleTradingMode(Guid id, [FromBody] ToggleTradingModeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new ToggleTradingModeCommand(id, request.TradingMode), cancellationToken);
            return Ok(ApiResponse<BrokerConnectionDto>.Ok(result, $"Trading mode changed to {request.TradingMode}."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("connections/{id:guid}/activate-paper")]
    [ProducesResponseType(typeof(ApiResponse<BrokerConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivatePaperConnection(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new ActivatePaperConnectionCommand(id), cancellationToken);
            return Ok(ApiResponse<BrokerConnectionDto>.Ok(result, "Paper trading mode activated. Engine is ready."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("connections/{id:guid}/manual-token")]
    [ProducesResponseType(typeof(ApiResponse<BrokerConnectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetManualToken(Guid id, [FromBody] SetManualTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new SetManualTokenCommand(id, request.AccessToken, request.ExpiresAtUtc), cancellationToken);
            return Ok(ApiResponse<BrokerConnectionDto>.Ok(result, "Access token applied successfully. Engine is ready."));
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("connections/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteConnection(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBrokerConnectionCommand(id), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, result ? "Broker connection removed." : "Connection not found."));
    }
}
