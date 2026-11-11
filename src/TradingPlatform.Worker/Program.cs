using Serilog;
using TradingPlatform.Application;
using TradingPlatform.Infrastructure;
using TradingPlatform.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

builder.Services.AddSerilog((services, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

try
{
    Log.Information("Starting TradingPlatform.Worker host");

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<ApprovalTimeoutProcessor>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "TradingPlatform.Worker terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
