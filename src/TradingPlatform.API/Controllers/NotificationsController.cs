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
public class NotificationsController(
    INotificationRepository notificationRepository) : ControllerBase
{
    private Guid GetUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(idStr, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<IReadOnlyList<NotificationDto>>.Fail("Unauthorized user"));
        }

        var (items, total) = await notificationRepository.GetUserNotificationsAsync(userId, unreadOnly, page, pageSize, ct);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(items, $"Retrieved {items.Count} notifications (Total: {total})"));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<int>.Fail("Unauthorized user"));
        }

        var count = await notificationRepository.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized user"));
        }

        await notificationRepository.MarkAsReadAsync(id, userId, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Marked notification as read"));
    }

    [HttpPut("mark-all-read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead(CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized user"));
        }

        await notificationRepository.MarkAllAsReadAsync(userId, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Marked all notifications as read"));
    }
}
