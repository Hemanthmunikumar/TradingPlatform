using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TradingPlatform.API.Hubs;

[Authorize]
public class TradingHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinAccountGroup(string accountId)
    {
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"account-{accountId}");
        }
    }
}
