using Microsoft.EntityFrameworkCore;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Application.Common;

public interface ITradingDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<BrokerProvider> BrokerProviders { get; }
    DbSet<BrokerConnection> BrokerConnections { get; }
    DbSet<TradingAccount> TradingAccounts { get; }

    DbSet<AccountConfiguration> AccountConfigurations { get; }
    DbSet<RiskProfile> RiskProfiles { get; }

    DbSet<Instrument> Instruments { get; }
    DbSet<InstrumentUniverse> InstrumentUniverses { get; }
    DbSet<InstrumentUniverseItem> InstrumentUniverseItems { get; }
    DbSet<MarketCandle> MarketCandles { get; }

    DbSet<StrategyDefinition> StrategyDefinitions { get; }
    DbSet<StrategyVersion> StrategyVersions { get; }
    DbSet<StrategyAccountMapping> StrategyAccountMappings { get; }

    DbSet<TradingSignal> TradingSignals { get; }
    DbSet<PendingTradeApproval> PendingTradeApprovals { get; }
    DbSet<TradingOrder> TradingOrders { get; }
    DbSet<OrderEvent> OrderEvents { get; }
    DbSet<TradeExecution> TradeExecutions { get; }
    DbSet<Position> Positions { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<DailyReport> DailyReports { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<WorkerHeartbeat> WorkerHeartbeats { get; }
    DbSet<SystemConfiguration> SystemConfigurations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
