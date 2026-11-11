using System.Security.Claims;
using TradingPlatform.Application.Common;

namespace TradingPlatform.API.Services;

public class HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _user?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? UserEmail => _user?.FindFirst(ClaimTypes.Email)?.Value
        ?? _user?.FindFirst("email")?.Value;

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => _user?.IsInRole("Admin") == true;
}
