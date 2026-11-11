using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.Application.Interfaces.Trades;

public interface ITradeHistoryService
{
    Task<IReadOnlyList<TradeHistoryDto>> GetTradeHistoryAsync(TradeHistoryFilterRequest filter, CancellationToken ct = default);
    Task<byte[]> ExportTradeHistoryCsvAsync(TradeHistoryFilterRequest filter, CancellationToken ct = default);
}
