using System.Text;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Interfaces.Trades;
using TradingPlatform.Contracts.Trades;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Trades;

public class TradeHistoryService(TradingDbContext dbContext) : ITradeHistoryService
{
    private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    public async Task<IReadOnlyList<TradeHistoryDto>> GetTradeHistoryAsync(TradeHistoryFilterRequest filter, CancellationToken ct = default)
    {
        var query = dbContext.TradeExecutions
            .Include(t => t.Instrument)
            .Include(t => t.TradingOrder)
            .Where(t => t.TradingAccount.IsActive && t.TradingAccount.BrokerConnection.IsActive)
            .AsQueryable();

        if (filter.TradingAccountId.HasValue)
        {
            query = query.Where(t => t.TradingAccountId == filter.TradingAccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Symbol))
        {
            query = query.Where(t => t.Instrument.TradingSymbol == filter.Symbol);
        }

        if (!string.IsNullOrWhiteSpace(filter.Side) && Enum.TryParse<OrderSide>(filter.Side, true, out var side))
        {
            query = query.Where(t => t.Side == side);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(t => t.ExecutedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(t => t.ExecutedAtUtc <= filter.ToUtc.Value);
        }

        var executions = await query
            .OrderByDescending(t => t.ExecutedAtUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var list = new List<TradeHistoryDto>(executions.Count);
        foreach (var exec in executions)
        {
            decimal? entryPrice = null;
            decimal? realizedPnL = null;

            if (exec.Side == OrderSide.Sell)
            {
                var pos = await dbContext.Positions
                    .FirstOrDefaultAsync(p => p.TradingAccountId == exec.TradingAccountId &&
                                              p.InstrumentId == exec.InstrumentId, ct);
                if (pos != null)
                {
                    entryPrice = pos.AverageBuyPrice;
                    realizedPnL = pos.RealizedNetPnL;
                }
            }

            list.Add(new TradeHistoryDto(
                ExecutionId: exec.Id,
                OrderId: exec.TradingOrderId,
                TradingAccountId: exec.TradingAccountId,
                InstrumentId: exec.InstrumentId,
                Symbol: exec.Instrument.TradingSymbol,
                CompanyName: exec.Instrument.CompanyName,
                Side: exec.Side.ToString(),
                ProductType: exec.TradingOrder?.ProductType.ToString() ?? "MIS",
                Quantity: exec.Quantity,
                Price: exec.Price,
                TotalValue: exec.TotalValue,
                EntryPrice: entryPrice,
                RealizedPnL: realizedPnL,
                Charges: null,
                Exchange: exec.Exchange,
                ExecutedAtUtc: exec.ExecutedAtUtc
            ));
        }

        return list;
    }

    public async Task<byte[]> ExportTradeHistoryCsvAsync(TradeHistoryFilterRequest filter, CancellationToken ct = default)
    {
        // Unpaginated fetch for export
        var unpaginatedFilter = filter with { PageNumber = 1, PageSize = 10000 };
        var trades = await GetTradeHistoryAsync(unpaginatedFilter, ct);

        var sb = new StringBuilder();
        sb.AppendLine("ExecutionId,TimestampIST,Symbol,CompanyName,Side,ProductType,Quantity,ExecutionPrice,TotalValue,EntryPrice,RealizedPnL,Exchange");

        foreach (var t in trades)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(t.ExecutedAtUtc, IstZone);
            var entryStr = t.EntryPrice.HasValue ? t.EntryPrice.Value.ToString("F2") : "";
            var pnlStr = t.RealizedPnL.HasValue ? t.RealizedPnL.Value.ToString("F2") : "";

            sb.AppendLine($"\"{t.ExecutionId}\",\"{istTime:yyyy-MM-dd HH:mm:ss}\",\"{t.Symbol}\",\"{t.CompanyName}\",\"{t.Side}\",\"{t.ProductType}\",{t.Quantity},{t.Price:F2},{t.TotalValue:F2},{entryStr},{pnlStr},\"{t.Exchange}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
