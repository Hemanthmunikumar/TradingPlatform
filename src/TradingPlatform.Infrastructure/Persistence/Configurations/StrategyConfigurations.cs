using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.Persistence.Configurations;

public class StrategyDefinitionConfiguration : IEntityTypeConfiguration<StrategyDefinition>
{
    public void Configure(EntityTypeBuilder<StrategyDefinition> builder)
    {
        builder.HasKey(sd => sd.Id);
        builder.HasIndex(sd => new { sd.UserId, sd.Name });
        builder.Property(sd => sd.Name).HasMaxLength(64).IsRequired();
        builder.Property(sd => sd.Description).HasMaxLength(512);
    }
}

public class StrategyVersionConfiguration : IEntityTypeConfiguration<StrategyVersion>
{
    public void Configure(EntityTypeBuilder<StrategyVersion> builder)
    {
        builder.HasKey(sv => sv.Id);
        builder.HasIndex(sv => new { sv.StrategyDefinitionId, sv.VersionNumber }).IsUnique();
        builder.Property(sv => sv.ConfigurationJson).HasColumnType("jsonb").IsRequired();
        builder.Property(sv => sv.StockUniverseJson).HasColumnType("jsonb");
        builder.Property(sv => sv.CapitalAllocation).HasPrecision(18, 4);
        builder.Property(sv => sv.RiskPerTradePercent).HasPrecision(5, 2);
        builder.Property(sv => sv.StopLossPercent).HasPrecision(5, 2);
        builder.Property(sv => sv.TargetPercent).HasPrecision(5, 2);

        builder.HasOne(sv => sv.StrategyDefinition)
            .WithMany(sd => sd.Versions)
            .HasForeignKey(sv => sv.StrategyDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StrategyAccountMappingConfiguration : IEntityTypeConfiguration<StrategyAccountMapping>
{
    public void Configure(EntityTypeBuilder<StrategyAccountMapping> builder)
    {
        builder.HasKey(sam => sam.Id);
        builder.HasIndex(sam => new { sam.StrategyVersionId, sam.TradingAccountId }).IsUnique();

        builder.HasOne(sam => sam.StrategyVersion)
            .WithMany(sv => sv.AccountMappings)
            .HasForeignKey(sam => sam.StrategyVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sam => sam.TradingAccount)
            .WithMany()
            .HasForeignKey(sam => sam.TradingAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
