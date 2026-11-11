using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.API.Hubs;

[Authorize]
public class TradingHub : Hub<ITradingHubClient>
{
    private readonly ILogger<TradingHub> _logger;

    public TradingHub(ILogger<TradingHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("SignalR Client connected: {ConnectionId} bound to user-{UserId}", Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        _logger.LogInformation("SignalR Client disconnected: {ConnectionId} (user: {UserId})", Context.ConnectionId, userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinAccountGroup(string accountId)
    {
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"account-{accountId}");
            _logger.LogInformation("Connection {ConnectionId} joined account-{AccountId}", Context.ConnectionId, accountId);
        }
    }

    public async Task LeaveAccountGroup(string accountId)
    {
        if (!string.IsNullOrWhiteSpace(accountId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"account-{accountId}");
            _logger.LogInformation("Connection {ConnectionId} left account-{AccountId}", Context.ConnectionId, accountId);
        }
    }

    public async Task JoinInstrumentGroup(string symbol)
    {
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"symbol-{symbol.ToUpperInvariant()}");
        }
    }

    public async Task LeaveInstrumentGroup(string symbol)
    {
        if (!string.IsNullOrWhiteSpace(symbol))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"symbol-{symbol.ToUpperInvariant()}");
        }
    }
}
