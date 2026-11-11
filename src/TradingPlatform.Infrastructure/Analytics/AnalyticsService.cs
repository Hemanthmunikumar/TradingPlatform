using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Analytics;
using TradingPlatform.Contracts.Analytics;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Analytics;

public class AnalyticsService(
    TradingDbContext dbContext,
    ILogger<AnalyticsService> logger) : IAnalyticsService
{
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    public async Task<PortfolioAnalyticsDto> GetPortfolioAnalyticsAsync(
        Guid userId,
        Guid? tradingAccountId = null,
        int days = 30,
        CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var startUtc = nowUtc.AddDays(-days);

        // Fetch accounts for capital valuation
        var accountQuery = dbContext.TradingAccounts.AsNoTracking().Where(a => a.UserId == userId);
        if (tradingAccountId.HasValue)
        {
            accountQuery = accountQuery.Where(a => a.Id == tradingAccountId.Value);
        }
        var accounts = await accountQuery.ToListAsync(ct);
        var totalAvailableFunds = accounts.Sum(a => a.AvailableFunds);
        var totalAllocatedCapital = accounts.Sum(a => a.AllocatedCapital);

        // Fetch open and closed positions
        var positionsQuery = dbContext.Positions
            .Include(p => p.Instrument)
            .Include(p => p.StrategyVersion)
            .ThenInclude(sv => sv!.StrategyDefinition)
            .AsNoTracking()
            .Where(p => p.UserId == userId);

        if (tradingAccountId.HasValue)
        {
            positionsQuery = positionsQuery.Where(p => p.TradingAccountId == tradingAccountId.Value);
        }

        var allPositions = await positionsQuery.ToListAsync(ct);

        var openPositions = allPositions.Where(p => p.IsOpen).ToList();
        var closedPositions = allPositions.Where(p => !p.IsOpen).ToList();

        var totalUnrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL);
        var totalOpenInvested = openPositions.Sum(p => p.AverageBuyPrice * p.Quantity);
        var totalPortfolioValue = totalAvailableFunds + totalOpenInvested + totalUnrealizedPnL;

        var totalRealizedPnL = closedPositions.Sum(p => p.RealizedNetPnL);
        var netTotalPnL = totalRealizedPnL + totalUnrealizedPnL;

        // Today, Week, Month PnL (IST aligned)
        var istNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, IstZone);
        var istTodayStart = istNow.Date;
        var istWeekStart = istNow.Date.AddDays(-(int)istNow.DayOfWeek);
        var istMonthStart = new DateTime(istNow.Year, istNow.Month, 1);

        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(istTodayStart, IstZone);
        var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(istWeekStart, IstZone);
        var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(istMonthStart, IstZone);

        var todayPnL = closedPositions
            .Where(p => p.ClosedAtUtc >= todayStartUtc)
            .Sum(p => p.RealizedNetPnL) + totalUnrealizedPnL;

        var weekPnL = closedPositions
            .Where(p => p.ClosedAtUtc >= weekStartUtc)
            .Sum(p => p.RealizedNetPnL) + totalUnrealizedPnL;

        var monthPnL = closedPositions
            .Where(p => p.ClosedAtUtc >= monthStartUtc)
            .Sum(p => p.RealizedNetPnL) + totalUnrealizedPnL;

        // Win/loss metrics
        var totalTrades = closedPositions.Count;
        var wins = closedPositions.Where(p => p.RealizedNetPnL > 0).ToList();
        var losses = closedPositions.Where(p => p.RealizedNetPnL < 0).ToList();

        var winningTrades = wins.Count;
        var losingTrades = losses.Count;
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades * 100 : 0m;

        var totalWinAmount = wins.Sum(p => p.RealizedNetPnL);
        var totalLossAmount = Math.Abs(losses.Sum(p => p.RealizedNetPnL));

        var avgWin = winningTrades > 0 ? totalWinAmount / winningTrades : 0m;
        var avgLoss = losingTrades > 0 ? totalLossAmount / losingTrades : 0m;
        var profitFactor = totalLossAmount > 0 ? Math.Round(totalWinAmount / totalLossAmount, 2) : totalWinAmount > 0 ? 99.9m : 0m;

        // Daily trend data points
        var dailyPoints = new List<DailyPnLDataPointDto>();
        decimal runningCumulative = 0m;

        var groupedByDate = closedPositions
            .Where(p => p.ClosedAtUtc >= startUtc)
            .GroupBy(p => TimeZoneInfo.ConvertTimeFromUtc(p.ClosedAtUtc!.Value, IstZone).ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key);

        foreach (var grp in groupedByDate)
        {
            var dayRealized = grp.Sum(p => p.RealizedNetPnL);
            runningCumulative += dayRealized;
            dailyPoints.Add(new DailyPnLDataPointDto(grp.Key, dayRealized, runningCumulative));
        }

        // Drawdown calculation from daily points
        decimal peak = 0m;
        decimal maxDrawdown = 0m;
        foreach (var pt in dailyPoints)
        {
            if (pt.CumulativePnL > peak) peak = pt.CumulativePnL;
            var dd = peak - pt.CumulativePnL;
            if (dd > maxDrawdown) maxDrawdown = dd;
        }
        var maxDrawdownPct = totalAllocatedCapital > 0 ? Math.Round((maxDrawdown / totalAllocatedCapital) * 100, 2) : 0m;

        // Symbol breakdown
        var symbolBreakdown = closedPositions
            .GroupBy(p => p.Instrument?.TradingSymbol ?? "UNKNOWN")
            .Select(g => new SymbolPnLSummaryDto(
                Symbol: g.Key,
                TradesCount: g.Count(),
                TotalQuantity: g.Sum(p => p.Quantity),
                RealizedPnL: g.Sum(p => p.RealizedGrossPnL),
                TotalCharges: g.Sum(p => p.TotalCharges),
                NetPnL: g.Sum(p => p.RealizedNetPnL)
            ))
            .OrderByDescending(s => s.NetPnL)
            .ToList();

        // Strategy breakdown
        var strategyBreakdown = closedPositions
            .GroupBy(p => p.StrategyVersion?.StrategyDefinition?.Name ?? "Manual Discretionary")
            .Select(g =>
            {
                var stratWins = g.Where(p => p.RealizedNetPnL > 0).ToList();
                var stratLosses = g.Where(p => p.RealizedNetPnL < 0).ToList();
                var sWinTotal = stratWins.Sum(p => p.RealizedNetPnL);
                var sLossTotal = Math.Abs(stratLosses.Sum(p => p.RealizedNetPnL));
                var sPf = sLossTotal > 0 ? Math.Round(sWinTotal / sLossTotal, 2) : sWinTotal > 0 ? 99.9m : 0m;
                var sWr = g.Any() ? (decimal)stratWins.Count / g.Count() * 100 : 0m;

                return new StrategyPerformanceDto(
                    StrategyName: g.Key,
                    TotalTrades: g.Count(),
                    WinningTrades: stratWins.Count,
                    LosingTrades: stratLosses.Count,
                    WinRatePercentage: Math.Round(sWr, 1),
                    RealizedNetPnL: g.Sum(p => p.RealizedNetPnL),
                    ProfitFactor: sPf
                );
            })
            .OrderByDescending(s => s.RealizedNetPnL)
            .ToList();

        return new PortfolioAnalyticsDto(
            UserId: userId,
            TradingAccountId: tradingAccountId,
            TotalPortfolioValue: totalPortfolioValue,
            TotalRealizedPnL: totalRealizedPnL,
            TotalUnrealizedPnL: totalUnrealizedPnL,
            NetTotalPnL: netTotalPnL,
            TodayPnL: todayPnL,
            WeekPnL: weekPnL,
            MonthPnL: monthPnL,
            TotalTrades: totalTrades,
            WinningTrades: winningTrades,
            LosingTrades: losingTrades,
            WinRatePercentage: Math.Round(winRate, 1),
            AverageWinAmount: Math.Round(avgWin, 2),
            AverageLossAmount: Math.Round(avgLoss, 2),
            ProfitFactor: profitFactor,
            MaxDrawdownPercentage: maxDrawdownPct,
            DailyPnLTrend: dailyPoints,
            SymbolBreakdown: symbolBreakdown,
            StrategyBreakdown: strategyBreakdown
        );
    }
}
