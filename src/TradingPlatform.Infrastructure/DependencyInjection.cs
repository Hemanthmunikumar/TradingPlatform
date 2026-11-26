using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Application.Interfaces.AI;
using TradingPlatform.Application.Interfaces.Analytics;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Application.Interfaces.Risk;
using TradingPlatform.Application.Interfaces.Strategies;
using TradingPlatform.Application.Interfaces.Trades;
using TradingPlatform.Infrastructure.Adapters;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Adapters.Upstox;
using TradingPlatform.Infrastructure.Adapters.Zerodha;
using TradingPlatform.Infrastructure.AI;
using TradingPlatform.Infrastructure.Analytics;
using TradingPlatform.Infrastructure.Approvals;
using TradingPlatform.Infrastructure.MarketData;
using TradingPlatform.Infrastructure.Notifications;
using TradingPlatform.Infrastructure.Orders;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Risk;
using TradingPlatform.Infrastructure.Security;
using TradingPlatform.Infrastructure.Strategies;
using TradingPlatform.Infrastructure.Trades;

namespace TradingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Security & Cryptography Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ICredentialEncryptionService, Aes256GcmEncryptionService>();

        // In-Memory Caching & Market Data Services
        services.AddMemoryCache();
        services.AddSingleton<IQuoteCache, QuoteCache>();
        services.AddScoped<IInstrumentService, InstrumentService>();
        services.AddScoped<IMarketDataService, MarketDataService>();

        // Strategy Engine & Technical Indicators
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<IStrategyEngine, StrategyEngine>();
        services.AddScoped<IBacktestingService, BacktestingService>();
        services.AddHttpClient<IAiMarketAdvisorService, ConfigurableAiMarketAdvisorService>();
        services.AddScoped<IIndiaDayTraderService, IndiaDayTraderService>();

        // Pre-Order Risk Validation & Safety Engine
        services.AddScoped<IRiskValidationService, RiskValidationService>();

        // Order Lifecycle, State Machine & Position Management
        services.AddSingleton<IIndianBrokerageCalculator, IndianBrokerageCalculator>();
        services.AddSingleton<IOrderStateMachine, OrderStateMachine>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IOrderService, OrderService>();

        // Trade Approvals & 2-Second BUY Rejection Workflow
        services.AddSingleton<ITradingNotificationService, LoggingTradingNotificationService>();
        services.AddScoped<ITradeApprovalService, TradeApprovalService>();

        // Trade History & Execution Reporting
        services.AddScoped<ITradeHistoryService, TradeHistoryService>();

        // Real-Time Notification Persistence
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Email, WhatsApp & Daily Reporting Services
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddHttpClient<IWhatsAppService, WhatsAppCloudApiService>();
        services.AddScoped<IDailyReportGeneratorService, DailyReportGeneratorService>();

        // Portfolio Analytics & Performance Engine
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        // Default current user service (overridden by API HttpContextCurrentUserService when registered)
        services.AddScoped<ICurrentUserService, DefaultCurrentUserService>();

        // Broker Adapters
        services.AddSingleton<MockBrokerAdapter>();
        services.AddHttpClient<UpstoxBrokerAdapter>();
        services.AddHttpClient<ZerodhaBrokerAdapter>();
        services.AddSingleton<IBrokerAdapterFactory, BrokerAdapterFactory>();

        // Register Database Context with EF Core (InMemory for zero-dependency local testing, Npgsql for PostgreSQL)
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);
        var connectionString = configuration.GetConnectionString("TradingDb") 
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TradingDbContext>(options =>
        {
            options.ConfigureWarnings(w => 
            {
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
            });

            if (useInMemory || string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("InMemory"))
            {
                options.UseInMemoryDatabase("TradingPlatform_Dev_InMemory");
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(TradingDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                });
                options.UseSnakeCaseNamingConvention();
            }
        });

        services.AddScoped<ITradingDbContext>(sp => sp.GetRequiredService<TradingDbContext>());

        return services;
    }
}
