using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Interfaces.AI;
using TradingPlatform.Application.Interfaces.Approvals;
using TradingPlatform.Contracts.Approvals;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.API.Services;

/// <summary>
/// Background market monitor that scans NSE F&O stocks during market hours (09:00-15:25 IST, Mon-Fri)
/// and raises Pending Trade Approvals + SignalR notifications.
/// Orders only auto-execute if AutoExecuteOnTimeout is explicitly enabled in account configuration.
/// </summary>
public class MarketMonitorHostedService(
    IServiceProvider serviceProvider,
    ILogger<MarketMonitorHostedService> logger) : BackgroundService
{
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    private readonly HashSet<string> _alertedToday = new(StringComparer.OrdinalIgnoreCase);
    private DateOnly _lastAlertDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MarketMonitorHostedService started (NSE 09:00-15:25 IST scanner, 2m cadence)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstZone);
                var today = DateOnly.FromDateTime(istNow);
                if (today != _lastAlertDate)
                {
                    _alertedToday.Clear();
                    _lastAlertDate = today;
                }

                // Run only on weekdays during NSE market hours (09:00 - 15:25 IST)
                if (istNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                var time = TimeOnly.FromDateTime(istNow);
                if (time < new TimeOnly(9, 0) || time > new TimeOnly(15, 25))
                    continue;

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
                var scanner = scope.ServiceProvider.GetRequiredService<IIndiaDayTraderService>();
                var approvals = scope.ServiceProvider.GetRequiredService<ITradeApprovalService>();

                var activeAccount = await db.TradingAccounts
                    .Include(a => a.BrokerConnection)
                    .Include(a => a.Configuration)
                    .Where(a => a.IsActive && a.TradingEnabled && a.BrokerConnection.IsActive)
                    .OrderByDescending(a => a.BrokerConnection.BrokerProvider.Code == BrokerCode.Zerodha)
                    .FirstOrDefaultAsync(stoppingToken);

                if (activeAccount == null || (activeAccount.Configuration?.KillSwitchActive ?? false))
                    continue;

                var minVol = activeAccount.Configuration?.IntradayMinVolume > 0
                    ? activeAccount.Configuration.IntradayMinVolume
                    : 20000;
                var duration = activeAccount.Configuration?.ApprovalDurationSeconds > 0
                    ? activeAccount.Configuration.ApprovalDurationSeconds
                    : 60;

                var scan = await scanner.ScanPreMarket21EmaAsync(1.0m, 2.5m, minVol, null, stoppingToken);
                foreach (var sig in scan.Signals.Where(s => s.Status == "ACTIVE_BUY_AT_21EMA"))
                {
                    var key = $"{today:yyyyMMdd}:{sig.Symbol}";
                    if (!_alertedToday.Add(key))
                        continue;

                    var inst = await db.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == sig.Symbol, stoppingToken);
                    if (inst == null)
                    {
                        inst = new TradingPlatform.Domain.Entities.Instrument
                        {
                            TradingSymbol = sig.Symbol,
                            Exchange = "NSE",
                            CompanyName = sig.Name,
                            LotSize = 1,
                            TickSize = 0.05m
                        };
                        db.Instruments.Add(inst);
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    await approvals.CreateApprovalAsync(new CreateTradeApprovalRequest(
                        TradingAccountId: activeAccount.Id,
                        InstrumentId: inst.Id,
                        TradingSignalId: null,
                        Symbol: sig.Symbol,
                        CompanyName: sig.Name,
                        Side: "Buy",
                        Quantity: 10,
                        IndicativePrice: sig.SpotPrice,
                        StopLoss: sig.StopLoss,
                        Target: sig.TargetPrice,
                        RiskAmount: Math.Round(Math.Abs(sig.SpotPrice - sig.StopLoss) * 10m, 2),
                        Reason: $"Live NSE Monitor: {sig.Symbol} (+{sig.GainPercent}%) touched 21-EMA (₹{sig.Ema21}). ATM: {sig.SuggestedAtmOption}, News: {sig.NewsSentiment}.",
                        ApprovalDurationSeconds: duration
                    ), stoppingToken);

                    logger.LogInformation("MarketMonitor raised {Duration}s BUY alert for {Symbol} (AutoExecute={Auto})",
                        duration, sig.Symbol, activeAccount.Configuration?.AutoExecuteOnTimeout ?? false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inside MarketMonitorHostedService");
            }
        }
    }
}
