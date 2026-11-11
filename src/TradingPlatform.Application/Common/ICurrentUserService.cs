namespace TradingPlatform.Application.Common;

/// <summary>
/// Ambient tenant and authenticated user context provider.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}
