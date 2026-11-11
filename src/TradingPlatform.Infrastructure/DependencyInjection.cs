using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Infrastructure.Adapters;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;

namespace TradingPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Security & Cryptography Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ICredentialEncryptionService, Aes256GcmEncryptionService>();

        // Default current user service (overridden by API HttpContextCurrentUserService when registered)
        services.AddScoped<ICurrentUserService, DefaultCurrentUserService>();

        // Broker Adapters
        services.AddSingleton<MockBrokerAdapter>();
        services.AddSingleton<IBrokerAdapterFactory, BrokerAdapterFactory>();

        // Register PostgreSQL with EF Core
        var connectionString = configuration.GetConnectionString("TradingDb")
            ?? "Host=localhost;Port=5432;Database=trading_platform_dev;Username=trading_user;Password=trading_dev_pass;Include Error Detail=true";

        services.AddDbContext<TradingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(TradingDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
            });
            options.UseSnakeCaseNamingConvention();
        });

        services.AddScoped<ITradingDbContext>(sp => sp.GetRequiredService<TradingDbContext>());

        return services;
    }
}
