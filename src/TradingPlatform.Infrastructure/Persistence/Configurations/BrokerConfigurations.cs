using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Persistence.Configurations;

public class BrokerProviderConfiguration : IEntityTypeConfiguration<BrokerProvider>
{
    public void Configure(EntityTypeBuilder<BrokerProvider> builder)
    {
        builder.HasKey(bp => bp.Id);
        builder.HasIndex(bp => bp.Code).IsUnique();
        builder.Property(bp => bp.Name).HasMaxLength(64).IsRequired();
        builder.Property(bp => bp.ApiBaseUrl).HasMaxLength(256).IsRequired();
        builder.Property(bp => bp.WebSocketUrl).HasMaxLength(256);
        builder.Property(bp => bp.AuthorizationUrl).HasMaxLength(256);
        builder.Property(bp => bp.TokenUrl).HasMaxLength(256);
        builder.Property(bp => bp.CapabilitiesJson).HasColumnType("jsonb");

        // Seed Supported Broker Providers
        builder.HasData(
            new BrokerProvider
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Mock Simulator",
                Code = BrokerCode.Mock,
                ApiBaseUrl = "http://localhost:5000/mock-broker",
                IsActive = true
            },
            new BrokerProvider
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Upstox",
                Code = BrokerCode.Upstox,
                ApiBaseUrl = "https://api.upstox.com/v2",
                WebSocketUrl = "wss://ws-feed.upstox.com/v2/feed",
                AuthorizationUrl = "https://api.upstox.com/v2/login/authorization/dialog",
                TokenUrl = "https://api.upstox.com/v2/login/authorization/token",
                IsActive = true
            },
            new BrokerProvider
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "Zerodha",
                Code = BrokerCode.Zerodha,
                ApiBaseUrl = "https://api.kite.trade",
                WebSocketUrl = "wss://ws.kite.trade",
                AuthorizationUrl = "https://kite.zerodha.com/connect/login",
                TokenUrl = "https://api.kite.trade/session/token",
                IsActive = true
            }
        );
    }
}

public class BrokerConnectionConfiguration : IEntityTypeConfiguration<BrokerConnection>
{
    public void Configure(EntityTypeBuilder<BrokerConnection> builder)
    {
        builder.HasKey(bc => bc.Id);
        builder.HasIndex(bc => new { bc.UserId, bc.BrokerProviderId });
        builder.Property(bc => bc.DisplayName).HasMaxLength(64).IsRequired();
        builder.Property(bc => bc.EncryptedApiKey).HasMaxLength(512).IsRequired();
        builder.Property(bc => bc.EncryptedApiSecret).HasMaxLength(512).IsRequired();
        builder.Property(bc => bc.EncryptedAccessToken).HasMaxLength(1024);
        builder.Property(bc => bc.EncryptedRefreshToken).HasMaxLength(1024);

        builder.HasOne(bc => bc.BrokerProvider)
            .WithMany(bp => bp.Connections)
            .HasForeignKey(bc => bc.BrokerProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TradingAccountConfiguration : IEntityTypeConfiguration<TradingAccount>
{
    public void Configure(EntityTypeBuilder<TradingAccount> builder)
    {
        builder.HasKey(ta => ta.Id);
        builder.HasIndex(ta => new { ta.BrokerConnectionId, ta.AccountId }).IsUnique();
        builder.HasIndex(ta => ta.UserId);
        builder.Property(ta => ta.AccountId).HasMaxLength(64).IsRequired();
        builder.Property(ta => ta.AccountName).HasMaxLength(64).IsRequired();
        builder.Property(ta => ta.AvailableFunds).HasPrecision(18, 4);
        builder.Property(ta => ta.AllocatedCapital).HasPrecision(18, 4);

        builder.HasOne(ta => ta.BrokerConnection)
            .WithMany(bc => bc.TradingAccounts)
            .HasForeignKey(ta => ta.BrokerConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ta => ta.Configuration)
            .WithOne(ac => ac.TradingAccount)
            .HasForeignKey<AccountConfiguration>(ac => ac.TradingAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
