using TradingPlatform.Application.Common;

namespace TradingPlatform.Infrastructure.Security;

/// <summary>
/// Default ambient user accessor for worker services and migrations.
/// In API, this is replaced by the HttpContext-based CurrentUserService in Milestone 3.
/// </summary>
public class DefaultCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public bool IsAuthenticated => UserId.HasValue;
    public bool IsAdmin { get; set; }
}
