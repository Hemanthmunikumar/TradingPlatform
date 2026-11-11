namespace TradingPlatform.Domain.Common;

/// <summary>
/// Marker interface for domain events dispatched on entity state changes.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
