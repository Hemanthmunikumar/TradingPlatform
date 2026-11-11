namespace TradingPlatform.Domain.Common;

/// <summary>
/// Enforces multi-tenant data isolation on all user-owned entities.
/// Automatically filtered by EF Core global query filters.
/// </summary>
public interface IMustHaveTenant
{
    Guid UserId { get; set; }
}

/// <summary>
/// Soft deletion support for durable audit retention.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
