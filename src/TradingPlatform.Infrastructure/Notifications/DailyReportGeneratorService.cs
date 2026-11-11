using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Notifications;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Notifications;

public class DailyReportGeneratorService(
    TradingDbContext dbContext,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    ILogger<DailyReportGeneratorService> logger) : IDailyReportGeneratorService
{
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    public async Task<DailyTradingReportDto> GenerateDailyReportAsync(
        Guid userId,
        DateOnly reportDate,
        Guid? tradingAccountId = null,
        CancellationToken ct = default)
    {
        // Compute start and end UTC bounds for the IST day
        var istStart = reportDate.ToDateTime(new TimeOnly(0, 0, 0));
        var istEnd = reportDate.ToDateTime(new TimeOnly(23, 59, 59));
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(istStart, IstZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(istEnd, IstZone);

        // Fetch positions closed during this window for realized P&L metrics
        var closedPositionsQuery = dbContext.Positions
            .Include(p => p.Instrument)
            .AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsOpen && p.ClosedAtUtc >= startUtc && p.ClosedAtUtc <= endUtc);

        if (tradingAccountId.HasValue)
        {
            closedPositionsQuery = closedPositionsQuery.Where(p => p.TradingAccountId == tradingAccountId.Value);
        }

        var closedPositions = await closedPositionsQuery.ToListAsync(ct);

        var totalTrades = closedPositions.Count;
        var winningTrades = closedPositions.Count(p => p.RealizedNetPnL > 0);
        var losingTrades = closedPositions.Count(p => p.RealizedNetPnL < 0);

        var grossPnL = closedPositions.Sum(p => p.RealizedGrossPnL);
        var totalCharges = closedPositions.Sum(p => p.TotalCharges);
        var netRealizedPnL = closedPositions.Sum(p => p.RealizedNetPnL);

        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades * 100 : 0m;

        var tradeItems = closedPositions.Select(p =>
        {
            var exitTimeIst = p.ClosedAtUtc.HasValue
                ? TimeZoneInfo.ConvertTimeFromUtc(p.ClosedAtUtc.Value, IstZone).ToString("yyyy-MM-dd HH:mm:ss")
                : "N/A";

            return new DailyTradeSummaryItemDto(
                Symbol: p.Instrument?.TradingSymbol ?? "UNKNOWN",
                Side: p.Side.ToString(),
                Quantity: p.Quantity,
                EntryPrice: p.AverageBuyPrice,
                ExitPrice: p.CurrentPrice,
                NetPnL: p.RealizedNetPnL,
                ExitReason: p.ExitReason ?? "Exit",
                ExecutedTimeIst: exitTimeIst
            );
        }).ToList();

        var report = new DailyTradingReportDto(
            ReportDate: reportDate,
            UserId: userId,
            TradingAccountId: tradingAccountId,
            TotalTrades: totalTrades,
            WinningTrades: winningTrades,
            LosingTrades: losingTrades,
            GrossPnL: grossPnL,
            TotalCharges: totalCharges,
            NetRealizedPnL: netRealizedPnL,
            WinRatePercentage: winRate,
            Trades: tradeItems,
            GeneratedAtUtc: DateTime.UtcNow
        );

        // Persist DailyReport record
        var existing = await dbContext.DailyReports
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReportDate == reportDate && r.TradingAccountId == tradingAccountId, ct);

        if (existing == null)
        {
            dbContext.DailyReports.Add(new DailyReport
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TradingAccountId = tradingAccountId,
                ReportDate = reportDate,
                ReportType = "DAILY_TRADING_DIGEST",
                ContentJson = JsonSerializer.Serialize(report),
                GeneratedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.ContentJson = JsonSerializer.Serialize(report);
            existing.GeneratedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        return report;
    }

    public async Task DispatchDailyReportsAsync(DateOnly reportDate, CancellationToken ct = default)
    {
        var users = await dbContext.Users.AsNoTracking().ToListAsync(ct);

        foreach (var user in users)
        {
            try
            {
                var report = await GenerateDailyReportAsync(user.Id, reportDate, null, ct);

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    await emailService.SendDailyReportEmailAsync(user.Email, report, ct);
                }

                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await whatsAppService.SendDailyReportWhatsAppAsync(user.PhoneNumber, report, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate/dispatch daily report for user {UserId}", user.Id);
            }
        }
    }
}
