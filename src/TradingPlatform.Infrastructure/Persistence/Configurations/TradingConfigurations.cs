using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.Persistence.Configurations;

public class AccountConfigurationConfiguration : IEntityTypeConfiguration<AccountConfiguration>
{
    public void Configure(EntityTypeBuilder<AccountConfiguration> builder)
    {
        builder.HasKey(ac => ac.Id);
        builder.HasIndex(ac => ac.TradingAccountId).IsUnique();
        builder.Property(ac => ac.MaxCapitalAllocation).HasPrecision(18, 4);
        builder.Property(ac => ac.MaxOrderValue).HasPrecision(18, 4);
        builder.Property(ac => ac.MaxRiskPerTradePercent).HasPrecision(5, 2);
        builder.Property(ac => ac.DailyLossLimit).HasPrecision(18, 4);
        builder.Property(ac => ac.MaxExposurePerStockPercent).HasPrecision(5, 2);

        builder.Property(ac => ac.AllowedTradingSegment).HasMaxLength(32).HasDefaultValue("Both");
        builder.Property(ac => ac.IntradayStopLossPercent).HasPrecision(5, 2);
        builder.Property(ac => ac.IntradayTargetPercent).HasPrecision(5, 2);
        builder.Property(ac => ac.IntradayMaxCapitalAllocation).HasPrecision(18, 4);
        builder.Property(ac => ac.IntradayMaxOrderValue).HasPrecision(18, 4);
        builder.Property(ac => ac.IntradayDailyLossLimit).HasPrecision(18, 4);

        builder.Property(ac => ac.DeliveryStopLossPercent).HasPrecision(5, 2);
        builder.Property(ac => ac.DeliveryTargetPercent).HasPrecision(5, 2);
        builder.Property(ac => ac.DeliveryMaxCapitalAllocation).HasPrecision(18, 4);
        builder.Property(ac => ac.DeliveryMaxOrderValue).HasPrecision(18, 4);
        builder.Property(ac => ac.DeliveryDailyLossLimit).HasPrecision(18, 4);
    }
}

public class RiskProfileConfiguration : IEntityTypeConfiguration<RiskProfile>
{
    public void Configure(EntityTypeBuilder<RiskProfile> builder)
    {
        builder.HasKey(rp => rp.Id);
        builder.HasIndex(rp => rp.TradingAccountId);
        builder.Property(rp => rp.ProfileName).HasMaxLength(64).IsRequired();
        builder.Property(rp => rp.MaxCapital).HasPrecision(18, 4);
        builder.Property(rp => rp.MaxDailyLoss).HasPrecision(18, 4);
        builder.Property(rp => rp.MaxPositionSize).HasPrecision(18, 4);
    }
}

public class PendingTradeApprovalConfiguration : IEntityTypeConfiguration<PendingTradeApproval>
{
    public void Configure(EntityTypeBuilder<PendingTradeApproval> builder)
    {
        builder.HasKey(pa => pa.Id);
        builder.HasIndex(pa => pa.IdempotencyKey).IsUnique();
        builder.HasIndex(pa => new { pa.Status, pa.ApprovalDeadlineUtc });
        builder.HasIndex(pa => pa.TradingAccountId);
        builder.Property(pa => pa.Symbol).HasMaxLength(32).IsRequired();
        builder.Property(pa => pa.CompanyName).HasMaxLength(128).IsRequired();
        builder.Property(pa => pa.IndicativePrice).HasPrecision(18, 4);
        builder.Property(pa => pa.StopLoss).HasPrecision(18, 4);
        builder.Property(pa => pa.Target).HasPrecision(18, 4);
        builder.Property(pa => pa.RiskAmount).HasPrecision(18, 4);
        builder.Property(pa => pa.Reason).HasMaxLength(512);

        builder.HasOne(pa => pa.TradingAccount)
            .WithMany()
            .HasForeignKey(pa => pa.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TradingOrderConfiguration : IEntityTypeConfiguration<TradingOrder>
{
    public void Configure(EntityTypeBuilder<TradingOrder> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.IdempotencyKey).IsUnique();
        builder.HasIndex(o => o.BrokerOrderId);
        builder.HasIndex(o => new { o.TradingAccountId, o.Status });
        builder.Property(o => o.BrokerOrderId).HasMaxLength(64);
        builder.Property(o => o.Price).HasPrecision(18, 4);
        builder.Property(o => o.TriggerPrice).HasPrecision(18, 4);
        builder.Property(o => o.AveragePrice).HasPrecision(18, 4);
        builder.Property(o => o.StatusMessage).HasMaxLength(512);

        builder.HasOne(o => o.TradingAccount)
            .WithMany(ta => ta.Orders)
            .HasForeignKey(o => o.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Instrument)
            .WithMany()
            .HasForeignKey(o => o.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.PendingTradeApproval)
            .WithMany()
            .HasForeignKey(o => o.PendingTradeApprovalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderEventConfiguration : IEntityTypeConfiguration<OrderEvent>
{
    public void Configure(EntityTypeBuilder<OrderEvent> builder)
    {
        builder.HasKey(oe => oe.Id);
        builder.HasIndex(oe => oe.TradingOrderId);
        builder.Property(oe => oe.EventType).HasMaxLength(64).IsRequired();
        builder.Property(oe => oe.BrokerResponseJson).HasColumnType("jsonb");

        builder.HasOne(oe => oe.TradingOrder)
            .WithMany(o => o.Events)
            .HasForeignKey(oe => oe.TradingOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TradeExecutionConfiguration : IEntityTypeConfiguration<TradeExecution>
{
    public void Configure(EntityTypeBuilder<TradeExecution> builder)
    {
        builder.HasKey(te => te.Id);
        builder.HasIndex(te => te.TradingOrderId);
        builder.HasIndex(te => te.TradingAccountId);
        builder.Property(te => te.BrokerTradeId).HasMaxLength(64);
        builder.Property(te => te.Price).HasPrecision(18, 4);
        builder.Property(te => te.Exchange).HasMaxLength(16).HasDefaultValue("NSE");

        builder.HasOne(te => te.TradingOrder)
            .WithMany(o => o.Executions)
            .HasForeignKey(te => te.TradingOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(te => te.Instrument)
            .WithMany()
            .HasForeignKey(te => te.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.TradingAccountId, p.IsOpen });
        builder.HasIndex(p => new { p.TradingAccountId, p.InstrumentId, p.IsOpen });
        builder.Property(p => p.AverageBuyPrice).HasPrecision(18, 4);
        builder.Property(p => p.CurrentPrice).HasPrecision(18, 4);
        builder.Property(p => p.StopLoss).HasPrecision(18, 4);
        builder.Property(p => p.Target).HasPrecision(18, 4);
        builder.Property(p => p.RealizedGrossPnL).HasPrecision(18, 4);
        builder.Property(p => p.RealizedNetPnL).HasPrecision(18, 4);
        builder.Property(p => p.TotalCharges).HasPrecision(18, 4);

        builder.HasOne(p => p.TradingAccount)
            .WithMany(ta => ta.Positions)
            .HasForeignKey(p => p.TradingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Instrument)
            .WithMany()
            .HasForeignKey(p => p.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TradingSignalConfiguration : IEntityTypeConfiguration<TradingSignal>
{
    public void Configure(EntityTypeBuilder<TradingSignal> builder)
    {
        builder.HasKey(ts => ts.Id);
        builder.HasIndex(ts => new { ts.TradingAccountId, ts.Status });
        builder.Property(ts => ts.Price).HasPrecision(18, 4);
        builder.Property(ts => ts.StopLoss).HasPrecision(18, 4);
        builder.Property(ts => ts.Target).HasPrecision(18, 4);
        builder.Property(ts => ts.Confidence).HasPrecision(5, 2);
        builder.Property(ts => ts.IndicatorValuesJson).HasColumnType("jsonb");

        builder.HasOne(ts => ts.Instrument)
            .WithMany()
            .HasForeignKey(ts => ts.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ts => ts.PendingApproval)
            .WithOne(pa => pa.TradingSignal)
            .HasForeignKey<PendingTradeApproval>(pa => pa.TradingSignalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
