using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.MarketData;
using TradingPlatform.Contracts.MarketData;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Infrastructure.MarketData;

public class InstrumentService(ITradingDbContext dbContext, ICurrentUserService currentUserService) : IInstrumentService
{
    private static readonly (string Symbol, string Name, string Token, string Isin, decimal BasePrice)[] DefaultNiftyStocks =
    [
        ("RELIANCE", "Reliance Industries Limited", "738561", "INE002A01018", 2950.0m),
        ("TCS", "Tata Consultancy Services Limited", "2953217", "INE467B01029", 4120.0m),
        ("HDFCBANK", "HDFC Bank Limited", "341249", "INE040A01034", 1540.0m),
        ("INFY", "Infosys Limited", "408065", "INE009A01021", 1850.0m),
        ("ICICIBANK", "ICICI Bank Limited", "1270529", "INE090A01021", 1210.0m),
        ("BHARTIARTL", "Bharti Airtel Limited", "2714625", "INE397D01024", 1620.0m),
        ("SBIN", "State Bank of India", "779521", "INE062A01020", 830.0m),
        ("ITC", "ITC Limited", "424961", "INE154A01025", 485.0m),
        ("LT", "Larsen & Toubro Limited", "2939649", "INE018A01030", 3650.0m),
        ("HINDUNILVR", "Hindustan Unilever Limited", "356865", "INE030A01027", 2480.0m),
        ("KOTAKBANK", "Kotak Mahindra Bank Limited", "492033", "INE237A01028", 1780.0m),
        ("AXISBANK", "Axis Bank Limited", "1510401", "INE238A01034", 1190.0m),
        ("BAJFINANCE", "Bajaj Finance Limited", "81153", "INE296A01024", 6950.0m),
        ("MARUTI", "Maruti Suzuki India Limited", "2815745", "INE585B01010", 11200.0m),
        ("TATAMOTORS", "Tata Motors Limited", "884737", "INE155A01022", 980.0m),
        ("SUNPHARMA", "Sun Pharmaceutical Industries Limited", "857857", "INE044A01036", 1830.0m),
        ("TITAN", "Titan Company Limited", "897281", "INE280A01028", 3420.0m),
        ("WIPRO", "Wipro Limited", "969473", "INE075A01022", 560.0m),
        ("ADANIENT", "Adani Enterprises Limited", "6401", "INE423A01024", 2890.0m),
        ("NTPC", "NTPC Limited", "2977281", "INE733E01010", 395.0m)
    ];

    public async Task<IReadOnlyList<InstrumentDto>> SearchInstrumentsAsync(SearchInstrumentsRequest request, CancellationToken ct = default)
    {
        // Auto-seed if instruments table is completely empty
        if (!await dbContext.Instruments.AnyAsync(ct))
        {
            await SeedDefaultInstrumentsAsync(ct);
        }

        var query = dbContext.Instruments.AsNoTracking().Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Exchange))
        {
            var ex = request.Exchange.Trim().ToUpperInvariant();
            query = query.Where(i => i.Exchange == ex);
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim().ToUpperInvariant();
            query = query.Where(i => i.TradingSymbol.Contains(q) || i.CompanyName.ToUpper().Contains(q));
        }

        var limit = Math.Clamp(request.Limit, 1, 100);
        var results = await query
            .OrderBy(i => i.TradingSymbol)
            .Take(limit)
            .Select(i => new InstrumentDto(
                i.Id,
                i.Exchange,
                i.TradingSymbol,
                i.InstrumentToken,
                i.ISIN,
                i.CompanyName,
                i.InstrumentType,
                i.LotSize,
                i.TickSize,
                i.Segment,
                i.IsActive
            ))
            .ToListAsync(ct);

        return results;
    }

    public async Task<InstrumentDto?> GetInstrumentByIdAsync(Guid id, CancellationToken ct = default)
    {
        var i = await dbContext.Instruments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (i == null) return null;

        return new InstrumentDto(
            i.Id,
            i.Exchange,
            i.TradingSymbol,
            i.InstrumentToken,
            i.ISIN,
            i.CompanyName,
            i.InstrumentType,
            i.LotSize,
            i.TickSize,
            i.Segment,
            i.IsActive
        );
    }

    public async Task<InstrumentDto?> GetByTradingSymbolAsync(string symbol, string exchange = "NSE", CancellationToken ct = default)
    {
        if (!await dbContext.Instruments.AnyAsync(ct))
        {
            await SeedDefaultInstrumentsAsync(ct);
        }

        var s = symbol.Trim().ToUpperInvariant();
        var ex = exchange.Trim().ToUpperInvariant();

        var i = await dbContext.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TradingSymbol == s && x.Exchange == ex, ct);

        if (i == null) return null;

        return new InstrumentDto(
            i.Id,
            i.Exchange,
            i.TradingSymbol,
            i.InstrumentToken,
            i.ISIN,
            i.CompanyName,
            i.InstrumentType,
            i.LotSize,
            i.TickSize,
            i.Segment,
            i.IsActive
        );
    }

    public async Task<IReadOnlyList<InstrumentUniverseDto>> GetUniversesAsync(CancellationToken ct = default)
    {
        var universes = await dbContext.InstrumentUniverses
            .AsNoTracking()
            .Include(u => u.Items)
                .ThenInclude(item => item.Instrument)
            .Where(u => u.IsActive)
            .ToListAsync(ct);

        return universes.Select(u => new InstrumentUniverseDto(
            u.Id,
            u.Name,
            u.Description,
            u.IsActive,
            u.Items.Where(it => it.Instrument != null).Select(it => new InstrumentDto(
                it.Instrument.Id,
                it.Instrument.Exchange,
                it.Instrument.TradingSymbol,
                it.Instrument.InstrumentToken,
                it.Instrument.ISIN,
                it.Instrument.CompanyName,
                it.Instrument.InstrumentType,
                it.Instrument.LotSize,
                it.Instrument.TickSize,
                it.Instrument.Segment,
                it.Instrument.IsActive
            )).ToList()
        )).ToList();
    }

    public async Task<InstrumentUniverseDto> CreateUniverseAsync(CreateInstrumentUniverseRequest request, CancellationToken ct = default)
    {
        var universe = new InstrumentUniverse
        {
            UserId = currentUserService.UserId ?? Guid.Empty,
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        foreach (var instId in request.InstrumentIds.Distinct())
        {
            universe.Items.Add(new InstrumentUniverseItem
            {
                InstrumentId = instId
            });
        }

        dbContext.InstrumentUniverses.Add(universe);
        await dbContext.SaveChangesAsync(ct);

        return (await GetUniversesAsync(ct)).First(u => u.Id == universe.Id);
    }

    public async Task<bool> AddInstrumentToUniverseAsync(Guid universeId, Guid instrumentId, CancellationToken ct = default)
    {
        var universe = await dbContext.InstrumentUniverses
            .Include(u => u.Items)
            .FirstOrDefaultAsync(u => u.Id == universeId, ct);

        if (universe == null) return false;

        if (universe.Items.Any(i => i.InstrumentId == instrumentId))
        {
            return true; // Already exists
        }

        universe.Items.Add(new InstrumentUniverseItem
        {
            InstrumentUniverseId = universeId,
            InstrumentId = instrumentId
        });

        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task SeedDefaultInstrumentsAsync(CancellationToken ct = default)
    {
        var existingSymbols = await dbContext.Instruments
            .Select(i => i.TradingSymbol)
            .ToHashSetAsync(ct);

        var newInstruments = new List<Instrument>();
        foreach (var s in DefaultNiftyStocks)
        {
            if (!existingSymbols.Contains(s.Symbol))
            {
                newInstruments.Add(new Instrument
                {
                    Exchange = "NSE",
                    TradingSymbol = s.Symbol,
                    CompanyName = s.Name,
                    InstrumentToken = s.Token,
                    ISIN = s.Isin,
                    InstrumentType = "EQUITY",
                    LotSize = 1,
                    TickSize = 0.05m,
                    Segment = "NSE_EQ",
                    IsActive = true,
                    LastUpdatedUtc = DateTime.UtcNow
                });
            }
        }

        if (newInstruments.Count > 0)
        {
            await dbContext.Instruments.AddRangeAsync(newInstruments, ct);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
