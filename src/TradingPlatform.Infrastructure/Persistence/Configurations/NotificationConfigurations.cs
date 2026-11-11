using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.UserId, n.IsRead });
        builder.Property(n => n.Type).HasMaxLength(32).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(128).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1024).IsRequired();
        builder.Property(n => n.DataJson).HasColumnType("jsonb");
    }
}

public class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        builder.HasKey(nd => nd.Id);
        builder.HasIndex(nd => new { nd.NotificationId, nd.Channel });

        builder.HasOne(nd => nd.Notification)
            .WithMany(n => n.Deliveries)
            .HasForeignKey(nd => nd.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.HasKey(dr => dr.Id);
        builder.HasIndex(dr => new { dr.UserId, dr.ReportDate }).IsUnique();
        builder.Property(dr => dr.ReportType).HasMaxLength(32).IsRequired();
        builder.Property(dr => dr.ContentJson).HasColumnType("jsonb").IsRequired();
        builder.Property(dr => dr.DeliveredViaJson).HasColumnType("jsonb");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(al => al.Id);
        builder.HasIndex(al => new { al.UserId, al.TimestampUtc });
        builder.HasIndex(al => new { al.EntityType, al.EntityId });
        builder.Property(al => al.Action).HasMaxLength(64).IsRequired();
        builder.Property(al => al.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(al => al.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(al => al.OldValuesJson).HasColumnType("jsonb");
        builder.Property(al => al.NewValuesJson).HasColumnType("jsonb");
        builder.Property(al => al.IpAddress).HasMaxLength(64);
        builder.Property(al => al.UserAgent).HasMaxLength(256);
    }
}

public class WorkerHeartbeatConfiguration : IEntityTypeConfiguration<WorkerHeartbeat>
{
    public void Configure(EntityTypeBuilder<WorkerHeartbeat> builder)
    {
        builder.HasKey(wh => wh.Id);
        builder.HasIndex(wh => wh.WorkerName);
        builder.Property(wh => wh.WorkerName).HasMaxLength(64).IsRequired();
        builder.Property(wh => wh.Status).HasMaxLength(32).IsRequired();
        builder.Property(wh => wh.MetadataJson).HasColumnType("jsonb");
    }
}

public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        builder.HasKey(sc => sc.Id);
        builder.HasIndex(sc => sc.Key).IsUnique();
        builder.Property(sc => sc.Key).HasMaxLength(64).IsRequired();
        builder.Property(sc => sc.Value).HasMaxLength(1024).IsRequired();
        builder.Property(sc => sc.Description).HasMaxLength(256);
    }
}
