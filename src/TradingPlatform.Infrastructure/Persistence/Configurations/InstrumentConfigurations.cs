using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.Persistence.Configurations;

public class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.Exchange, i.TradingSymbol }).IsUnique();
        builder.HasIndex(i => i.InstrumentToken);
        builder.Property(i => i.Exchange).HasMaxLength(16).HasDefaultValue("NSE");
        builder.Property(i => i.TradingSymbol).HasMaxLength(32).IsRequired();
        builder.Property(i => i.InstrumentToken).HasMaxLength(64).IsRequired();
        builder.Property(i => i.ISIN).HasMaxLength(32);
        builder.Property(i => i.CompanyName).HasMaxLength(128).IsRequired();
        builder.Property(i => i.InstrumentType).HasMaxLength(16).HasDefaultValue("EQUITY");
        builder.Property(i => i.TickSize).HasPrecision(18, 4);
        builder.Property(i => i.Segment).HasMaxLength(32).HasDefaultValue("NSE_EQ");
    }
}

public class InstrumentUniverseConfiguration : IEntityTypeConfiguration<InstrumentUniverse>
{
    public void Configure(EntityTypeBuilder<InstrumentUniverse> builder)
    {
        builder.HasKey(iu => iu.Id);
        builder.HasIndex(iu => new { iu.UserId, iu.Name });
        builder.Property(iu => iu.Name).HasMaxLength(64).IsRequired();
        builder.Property(iu => iu.Description).HasMaxLength(256);
    }
}

public class InstrumentUniverseItemConfiguration : IEntityTypeConfiguration<InstrumentUniverseItem>
{
    public void Configure(EntityTypeBuilder<InstrumentUniverseItem> builder)
    {
        builder.HasKey(iui => iui.Id);
        builder.HasIndex(iui => new { iui.InstrumentUniverseId, iui.InstrumentId }).IsUnique();

        builder.HasOne(iui => iui.Universe)
            .WithMany(iu => iu.Items)
            .HasForeignKey(iui => iui.InstrumentUniverseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(iui => iui.Instrument)
            .WithMany()
            .HasForeignKey(iui => iui.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MarketCandleConfiguration : IEntityTypeConfiguration<MarketCandle>
{
    public void Configure(EntityTypeBuilder<MarketCandle> builder)
    {
        builder.HasKey(mc => mc.Id);
        builder.HasIndex(mc => new { mc.InstrumentId, mc.Timeframe, mc.TimestampUtc }).IsUnique();
        builder.Property(mc => mc.Timeframe).HasMaxLength(8).IsRequired();
        builder.Property(mc => mc.Open).HasPrecision(18, 4);
        builder.Property(mc => mc.High).HasPrecision(18, 4);
        builder.Property(mc => mc.Low).HasPrecision(18, 4);
        builder.Property(mc => mc.Close).HasPrecision(18, 4);
        builder.Property(mc => mc.VWAP).HasPrecision(18, 4);

        builder.HasOne(mc => mc.Instrument)
            .WithMany()
            .HasForeignKey(mc => mc.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
