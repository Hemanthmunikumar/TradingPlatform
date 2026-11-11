using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Domain.Entities;

public class Notification : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? TradingAccountId { get; set; }
    public TradingAccount? TradingAccount { get; set; }

    public string Type { get; set; } = string.Empty; // BUY_APPROVAL, SELL_FILLED, SL_HIT, etc.
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public ICollection<NotificationDelivery> Deliveries { get; set; } = [];
}

public class NotificationDelivery : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public NotificationChannel Channel { get; set; } = NotificationChannel.SignalR;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public class DailyReport : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? TradingAccountId { get; set; }
    public TradingAccount? TradingAccount { get; set; }

    public DateOnly ReportDate { get; set; }
    public string ReportType { get; set; } = "DAILY_TRADING_DIGEST";
    public string ContentJson { get; set; } = "{}";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string? DeliveredViaJson { get; set; }
}

public class AuditLog : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class WorkerHeartbeat : BaseEntity
{
    public string WorkerName { get; set; } = "WorkerService";
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "HEALTHY";
    public string? MetadataJson { get; set; }
}

public class SystemConfiguration : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
