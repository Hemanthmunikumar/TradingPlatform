using MediatR;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;

namespace TradingPlatform.Application.Features.MarketData;

public record SearchInstrumentsQuery(SearchInstrumentsRequest Request) : IRequest<IReadOnlyList<InstrumentDto>>;

public class SearchInstrumentsHandler(IInstrumentService instrumentService) 
    : IRequestHandler<SearchInstrumentsQuery, IReadOnlyList<InstrumentDto>>
{
    public async Task<IReadOnlyList<InstrumentDto>> Handle(SearchInstrumentsQuery request, CancellationToken cancellationToken)
    {
        return await instrumentService.SearchInstrumentsAsync(request.Request, cancellationToken);
    }
}

public record GetInstrumentByIdQuery(Guid Id) : IRequest<InstrumentDto?>;

public class GetInstrumentByIdHandler(IInstrumentService instrumentService)
    : IRequestHandler<GetInstrumentByIdQuery, InstrumentDto?>
{
    public async Task<InstrumentDto?> Handle(GetInstrumentByIdQuery request, CancellationToken cancellationToken)
    {
        return await instrumentService.GetInstrumentByIdAsync(request.Id, cancellationToken);
    }
}

public record GetCandlesQuery(GetCandlesRequest Request) : IRequest<IReadOnlyList<MarketCandleDto>>;

public class GetCandlesHandler(IMarketDataService marketDataService)
    : IRequestHandler<GetCandlesQuery, IReadOnlyList<MarketCandleDto>>
{
    public async Task<IReadOnlyList<MarketCandleDto>> Handle(GetCandlesQuery request, CancellationToken cancellationToken)
    {
        return await marketDataService.GetHistoricalCandlesAsync(request.Request, cancellationToken);
    }
}

public record GetMarketQuoteQuery(string Symbol, string Exchange = "NSE") : IRequest<MarketQuoteDto>;

public class GetMarketQuoteHandler(IMarketDataService marketDataService)
    : IRequestHandler<GetMarketQuoteQuery, MarketQuoteDto>
{
    public async Task<MarketQuoteDto> Handle(GetMarketQuoteQuery request, CancellationToken cancellationToken)
    {
        return await marketDataService.GetQuoteAsync(request.Symbol, request.Exchange, cancellationToken);
    }
}

public record GetMarketStatusQuery(string Exchange = "NSE") : IRequest<MarketStatusDto>;

public class GetMarketStatusHandler(IMarketDataService marketDataService)
    : IRequestHandler<GetMarketStatusQuery, MarketStatusDto>
{
    public Task<MarketStatusDto> Handle(GetMarketStatusQuery request, CancellationToken cancellationToken)
    {
        var result = marketDataService.GetMarketStatus(request.Exchange);
        return Task.FromResult(result);
    }
}

public record GetInstrumentUniversesQuery : IRequest<IReadOnlyList<InstrumentUniverseDto>>;

public class GetInstrumentUniversesHandler(IInstrumentService instrumentService)
    : IRequestHandler<GetInstrumentUniversesQuery, IReadOnlyList<InstrumentUniverseDto>>
{
    public async Task<IReadOnlyList<InstrumentUniverseDto>> Handle(GetInstrumentUniversesQuery request, CancellationToken cancellationToken)
    {
        return await instrumentService.GetUniversesAsync(cancellationToken);
    }
}

public record CreateInstrumentUniverseCommand(CreateInstrumentUniverseRequest Request) : IRequest<InstrumentUniverseDto>;

public class CreateInstrumentUniverseHandler(IInstrumentService instrumentService)
    : IRequestHandler<CreateInstrumentUniverseCommand, InstrumentUniverseDto>
{
    public async Task<InstrumentUniverseDto> Handle(CreateInstrumentUniverseCommand request, CancellationToken cancellationToken)
    {
        return await instrumentService.CreateUniverseAsync(request.Request, cancellationToken);
    }
}
