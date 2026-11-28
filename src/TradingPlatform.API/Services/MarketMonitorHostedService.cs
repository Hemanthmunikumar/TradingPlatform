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

                    // Daily 10-day purge of resolved PendingTradeApprovals (safe FK unlink on TradingOrders first)
                    try
                    {
                        using var purgeScope = serviceProvider.CreateScope();
                        var purgeDb = purgeScope.ServiceProvider.GetRequiredService<TradingDbContext>();
                        var cutoffUtc = DateTime.UtcNow.AddDays(-10);

                        var oldIds = await purgeDb.PendingTradeApprovals
                            .Where(p => p.CreatedAtUtc < cutoffUtc && p.Status != TradingPlatform.Domain.Enums.ApprovalStatus.Pending)
                            .Select(p => p.Id)
                            .ToListAsync(stoppingToken);

                        if (oldIds.Count > 0)
                        {
                            await purgeDb.TradingOrders
                                .Where(o => o.PendingTradeApprovalId != null && oldIds.Contains(o.PendingTradeApprovalId.Value))
                                .ExecuteUpdateAsync(s => s.SetProperty(o => o.PendingTradeApprovalId, (Guid?)null), stoppingToken);

                            var deleted = await purgeDb.PendingTradeApprovals
                                .Where(p => oldIds.Contains(p.Id))
                                .ExecuteDeleteAsync(stoppingToken);

                            logger.LogInformation("Purged {Count} PendingTradeApprovals older than 10 days (before {Cutoff:yyyy-MM-dd}).", deleted, cutoffUtc);
                        }
                    }
                    catch (Exception purgeEx)
                    {
                        logger.LogWarning(purgeEx, "Failed running 10-day PendingTradeApprovals purge");
                    }
                }

                // Run only on weekdays
                if (istNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                var time = TimeOnly.FromDateTime(istNow);

                // Morning 06:05-08:55 IST reminder if Zerodha daily token expired at 06:00 IST
                if (time >= new TimeOnly(6, 5) && time < new TimeOnly(9, 0))
                {
                    var remKey = $"ZREM:{today:yyyyMMdd}";
                    if (_alertedToday.Add(remKey))
                    {
                        using var remScope = serviceProvider.CreateScope();
                        var remDb = remScope.ServiceProvider.GetRequiredService<TradingDbContext>();
                        var notifier = remScope.ServiceProvider.GetRequiredService<TradingPlatform.Application.Interfaces.Notifications.ITradingNotificationService>();
                        var zerodha = await remDb.BrokerConnections
                            .Include(bc => bc.BrokerProvider)
                            .FirstOrDefaultAsync(bc => bc.IsActive && bc.BrokerProvider.Code == BrokerCode.Zerodha, stoppingToken);

                        if (zerodha != null && (!zerodha.IsAuthenticated || !zerodha.TokenExpiresAtUtc.HasValue || zerodha.TokenExpiresAtUtc.Value <= DateTime.UtcNow))
                        {
                            await notifier.PublishSystemAlertAsync(
                                new TradingPlatform.Contracts.Notifications.SystemAlertNotificationDto(
                                    Level: "Warning",
                                    Title: "Zerodha Daily Session Expired (06:00 IST)",
                                    Message: "Zerodha invalidates Kite access tokens daily at 06:00 IST. Open Brokers & Accounts and click Re-authenticate before 09:00 IST.",
                                    TimestampUtc: DateTime.UtcNow),
                                stoppingToken);
                        }
                    }
                    continue;
                }

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

                var slPct = activeAccount.Configuration?.IntradayStopLossPercent > 0
                    ? activeAccount.Configuration.IntradayStopLossPercent
                    : 2.5m;
                var tgtPct = activeAccount.Configuration?.IntradayTargetPercent > 0
                    ? activeAccount.Configuration.IntradayTargetPercent
                    : 10.0m;
                var slFactor = 1m - (slPct / 100m);
                var tgtFactor = 1m + (tgtPct / 100m);

                var scan = await scanner.ScanPreMarket21EmaAsync(1.0m, 2.5m, minVol, null, stoppingToken);
                foreach (var sig in scan.Signals.Where(s => s.Status == "ACTIVE_BUY_AT_21EMA"))
                {
                    var key = $"{today:yyyyMMdd}:EMA21:{sig.Symbol}";
                    if (_alertedToday.Contains(key))
                        continue;

                    // Stage 2: News & Regulatory Red-Flag Gate (india-news)
                    var news = await scanner.GetStockNewsAsync(sig.Symbol, 24, stoppingToken);
                    if (news.HasRedFlag)
                    {
                        _alertedToday.Add(key);
                        logger.LogWarning("MarketMonitor skipped {Symbol} due to news red-flag: {Reason}", sig.Symbol, news.RedFlagReason);
                        continue;
                    }

                    // Stage 3: 5m Candle Close Confirmation Gate (india-entry)
                    var entryCheck = await scanner.CheckEntryConfirmationAsync(sig.Symbol, sig.Ema21, 1.2m, stoppingToken);
                    if (string.Equals(entryCheck.Status, "waiting", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation("MarketMonitor waiting for 5m candle close above 21-EMA for {Symbol}", sig.Symbol);
                        continue;
                    }

                    _alertedToday.Add(key);
                    var topHeadline = news.Items.FirstOrDefault()?.Title;
                    var newsTag = !string.IsNullOrWhiteSpace(topHeadline) ? $" | News: {topHeadline}" : $" | News: {sig.NewsSentiment}";
                    var entryTag = string.Equals(entryCheck.Status, "triggered", StringComparison.OrdinalIgnoreCase)
                        ? "5m Candle Confirmed"
                        : "21-EMA Pullback";

                    var slPrice = Math.Round(sig.SpotPrice * slFactor, 2);
                    var tgtPrice = Math.Round(sig.SpotPrice * tgtFactor, 2);

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
                        StopLoss: slPrice,
                        Target: tgtPrice,
                        RiskAmount: Math.Round(Math.Abs(sig.SpotPrice - slPrice) * 10m, 2),
                        Reason: $"Live NSE Monitor ({entryTag}): {sig.Symbol} (+{sig.GainPercent}%) near 21-EMA (₹{sig.Ema21}). ATM: {sig.SuggestedAtmOption}{newsTag}",
                        ApprovalDurationSeconds: duration
                    ), stoppingToken);

                    logger.LogInformation("MarketMonitor raised {Duration}s BUY alert for {Symbol} (AutoExecute={Auto}, Entry={EntryStatus})",
                        duration, sig.Symbol, activeAccount.Configuration?.AutoExecuteOnTimeout ?? false, entryCheck.Status);
                }

                // Also scan 15-min ORB Breakouts between 09:30 and 14:45 IST using all skills
                if (time >= new TimeOnly(9, 30) && time <= new TimeOnly(14, 45))
                {
                    var orbHits = await scanner.ScanOrbBreakoutsAsync(null, 20, 50000000, stoppingToken);
                    foreach (var hit in orbHits.Where(h => !h.NearUpperCircuit && h.Setups.Count > 0))
                    {
                        var orbKey = $"{today:yyyyMMdd}:ORB:{hit.Symbol}";
                        if (_alertedToday.Contains(orbKey))
                            continue;

                        var news = await scanner.GetStockNewsAsync(hit.Symbol, 24, stoppingToken);
                        if (news.HasRedFlag)
                        {
                            _alertedToday.Add(orbKey);
                            logger.LogWarning("MarketMonitor ORB skipped {Symbol} due to news red-flag: {Reason}", hit.Symbol, news.RedFlagReason);
                            continue;
                        }

                        var entryCheck = await scanner.CheckEntryConfirmationAsync(hit.Symbol, null, 1.2m, stoppingToken);
                        if (string.Equals(entryCheck.Status, "waiting", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        _alertedToday.Add(orbKey);
                        var slPrice = Math.Round(hit.Price * slFactor, 2);
                        var tgtPrice = Math.Round(hit.Price * tgtFactor, 2);
                        var inst = await db.Instruments.FirstOrDefaultAsync(i => i.TradingSymbol == hit.Symbol, stoppingToken);
                        if (inst == null)
                        {
                            inst = new TradingPlatform.Domain.Entities.Instrument
                            {
                                TradingSymbol = hit.Symbol,
                                Exchange = hit.Exchange,
                                CompanyName = hit.Symbol,
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
                            Symbol: hit.Symbol,
                            CompanyName: hit.Symbol,
                            Side: "Buy",
                            Quantity: 10,
                            IndicativePrice: hit.Price,
                            StopLoss: slPrice,
                            Target: tgtPrice,
                            RiskAmount: Math.Round(Math.Abs(hit.Price - slPrice) * 10m, 2),
                            Reason: $"15m ORB Breakout ({string.Join(", ", hit.Setups)}): {hit.Symbol} @ ₹{hit.Price} (RSI {hit.Rsi}).",
                            ApprovalDurationSeconds: duration
                        ), stoppingToken);

                        logger.LogInformation("MarketMonitor ORB raised {Duration}s BUY alert for {Symbol}", duration, hit.Symbol);
                    }
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
