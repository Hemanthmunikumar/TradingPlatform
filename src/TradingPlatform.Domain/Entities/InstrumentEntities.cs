using TradingPlatform.Domain.Common;

namespace TradingPlatform.Domain.Entities;

public class Instrument : BaseEntity
{
    public string Exchange { get; set; } = "NSE"; // NSE / BSE / NFO
    public string TradingSymbol { get; set; } = string.Empty; // e.g. "INFY", "RELIANCE"
    public string InstrumentToken { get; set; } = string.Empty; // Broker-assigned token
    public string? ISIN { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = "EQUITY"; // EQUITY / FUT / CE / PE
    public int LotSize { get; set; } = 1;
    public decimal TickSize { get; set; } = 0.05m;
    public string Segment { get; set; } = "NSE_EQ";
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

public class InstrumentUniverse : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // e.g. "Nifty 50 High Beta", "IT Momentum"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InstrumentUniverseItem> Items { get; set; } = [];
}

public class InstrumentUniverseItem : BaseEntity
{
    public Guid InstrumentUniverseId { get; set; }
    public InstrumentUniverse Universe { get; set; } = null!;

    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;
}

public class MarketCandle : BaseEntity
{
    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public string Timeframe { get; set; } = "5m"; // 1m, 5m, 15m, 1h, 1d
    public DateTime TimestampUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal? VWAP { get; set; }
}
