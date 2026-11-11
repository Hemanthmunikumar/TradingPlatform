using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.Persistence;

public class TradingDbContext(
    DbContextOptions<TradingDbContext> options,
    ICurrentUserService currentUserService) : DbContext(options)
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<BrokerProvider> BrokerProviders => Set<BrokerProvider>();
    public DbSet<BrokerConnection> BrokerConnections => Set<BrokerConnection>();
    public DbSet<TradingAccount> TradingAccounts => Set<TradingAccount>();

    public DbSet<AccountConfiguration> AccountConfigurations => Set<AccountConfiguration>();
    public DbSet<RiskProfile> RiskProfiles => Set<RiskProfile>();

    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<InstrumentUniverse> InstrumentUniverses => Set<InstrumentUniverse>();
    public DbSet<InstrumentUniverseItem> InstrumentUniverseItems => Set<InstrumentUniverseItem>();
    public DbSet<MarketCandle> MarketCandles => Set<MarketCandle>();

    public DbSet<StrategyDefinition> StrategyDefinitions => Set<StrategyDefinition>();
    public DbSet<StrategyVersion> StrategyVersions => Set<StrategyVersion>();
    public DbSet<StrategyAccountMapping> StrategyAccountMappings => Set<StrategyAccountMapping>();

    public DbSet<TradingSignal> TradingSignals => Set<TradingSignal>();
    public DbSet<PendingTradeApproval> PendingTradeApprovals => Set<PendingTradeApproval>();
    public DbSet<TradingOrder> TradingOrders => Set<TradingOrder>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<TradeExecution> TradeExecutions => Set<TradeExecution>();
    public DbSet<Position> Positions => Set<Position>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WorkerHeartbeat> WorkerHeartbeats => Set<WorkerHeartbeat>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply fluent entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);

        // Configure Global Query Filters for multi-tenant isolation and soft deletion
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // Multi-tenant isolation: filter by ambient user if entity implements IMustHaveTenant and user is not admin
            if (typeof(IMustHaveTenant).IsAssignableFrom(clrType))
            {
                var method = typeof(TradingDbContext)
                    .GetMethod(nameof(ConfigureTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }

            // Soft-delete filter
            if (typeof(ISoftDelete).IsAssignableFrom(clrType))
            {
                var method = typeof(TradingDbContext)
                    .GetMethod(nameof(ConfigureSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType);
                method.Invoke(this, [modelBuilder]);
            }
        }
    }

    private void ConfigureTenantFilter<T>(ModelBuilder builder) where T : class, IMustHaveTenant
    {
        // When user is authenticated and not admin, restrict queries strictly to own tenant
        builder.Entity<T>().HasQueryFilter(e =>
            _currentUserService.IsAdmin ||
            !_currentUserService.UserId.HasValue ||
            e.UserId == _currentUserService.UserId.Value);
    }

    private void ConfigureSoftDeleteFilter<T>(ModelBuilder builder) where T : class, ISoftDelete
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // Stamp tenant ID if not already set and user context is available
                if (entry.Entity is IMustHaveTenant tenantEntity && tenantEntity.UserId == Guid.Empty && _currentUserService.UserId.HasValue)
                {
                    tenantEntity.UserId = _currentUserService.UserId.Value;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdateTimestamp();
            }
        }

        // Handle soft deletes
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
