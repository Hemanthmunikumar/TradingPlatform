using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Application.Features.Brokers;
using TradingPlatform.Contracts.Brokers;
using TradingPlatform.Contracts.Common;

namespace TradingPlatform.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/trading-accounts")]
public class TradingAccountsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TradingAccountDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTradingAccountsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TradingAccountDto>>.Ok(result));
    }

    [HttpPut("{id:guid}/configuration")]
    [ProducesResponseType(typeof(ApiResponse<AccountConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConfiguration(Guid id, [FromBody] UpdateAccountConfigurationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new UpdateAccountConfigurationCommand(id, request), cancellationToken);
            return Ok(ApiResponse<AccountConfigurationDto>.Ok(result, "Account configuration updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
