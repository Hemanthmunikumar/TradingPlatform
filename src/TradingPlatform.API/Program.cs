using Serilog;
using TradingPlatform.Application;
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

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure HTTP Pipeline
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();

    // System and health endpoints
    app.MapHealthChecks("/health");

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
