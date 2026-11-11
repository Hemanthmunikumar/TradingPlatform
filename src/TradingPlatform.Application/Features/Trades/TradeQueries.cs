using MediatR;
using TradingPlatform.Application.Interfaces.Trades;
using TradingPlatform.Contracts.Trades;

namespace TradingPlatform.Application.Features.Trades;

public record GetTradeHistoryQuery(TradeHistoryFilterRequest Filter) : IRequest<IReadOnlyList<TradeHistoryDto>>;

public class GetTradeHistoryQueryHandler(ITradeHistoryService tradeService) : IRequestHandler<GetTradeHistoryQuery, IReadOnlyList<TradeHistoryDto>>
{
    public Task<IReadOnlyList<TradeHistoryDto>> Handle(GetTradeHistoryQuery request, CancellationToken cancellationToken)
    {
        return tradeService.GetTradeHistoryAsync(request.Filter, cancellationToken);
    }
}

public record ExportTradeHistoryCsvQuery(TradeHistoryFilterRequest Filter) : IRequest<byte[]>;

public class ExportTradeHistoryCsvQueryHandler(ITradeHistoryService tradeService) : IRequestHandler<ExportTradeHistoryCsvQuery, byte[]>
{
    public Task<byte[]> Handle(ExportTradeHistoryCsvQuery request, CancellationToken cancellationToken)
    {
        return tradeService.ExportTradeHistoryCsvAsync(request.Filter, cancellationToken);
    }
}
