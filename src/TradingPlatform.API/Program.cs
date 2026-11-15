using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using TradingPlatform.API.Hubs;
using TradingPlatform.API.Services;
using TradingPlatform.Application;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

try
{
    Log.Information("Starting TradingPlatform.API host");

    // Add Services to DI container
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register HttpContext ambient tenant accessor (overrides default fallback)
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

    // Configure JWT Bearer Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? "TradingPlatformSuperSecureDevelopmentSecretKey2026!MustBeAtLeast32Bytes";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TradingPlatform.API";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TradingPlatform.Client";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // set true in production via config
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHealthChecks();
    builder.Services.AddHostedService<ApprovalTimeoutHostedService>();

    // SignalR Real-Time Hub & Notification Service
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<ITradingNotificationService, SignalRTradingNotificationService>();

    // CORS for React Web / PWA
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Ensure database migration and initial metadata seed data exist
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TradingPlatform.Infrastructure.Persistence.TradingDbContext>();
        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }
        else
        {
            db.Database.EnsureCreated();
        }

        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        TradingPlatform.Infrastructure.Persistence.DatabaseSeeder.SeedAsync(db, scope.ServiceProvider, logger).GetAwaiter().GetResult();
    }

    // Configure HTTP Pipeline
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();

    // Security Response Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none';");
        await next();
    });

    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    // Endpoints
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.MapHub<TradingHub>("/hubs/trading");

    app.MapGet("/api/v1/system/status", () =>
    {
        var status = new
        {
            Service = "TradingPlatform.API",
            Framework = ".NET 10 LTS",
            Status = "Healthy",
            ServerTimeUtc = DateTime.UtcNow,
            Timezone = "Asia/Kolkata"
        };
        return Results.Ok(ApiResponse<object>.Ok(status, "System operational"));
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TradingPlatform.API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
