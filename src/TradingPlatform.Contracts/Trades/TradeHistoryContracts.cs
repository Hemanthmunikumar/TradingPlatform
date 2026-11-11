using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Contracts.Trades;

public record SellExecutionNotificationDto(
    Guid TradeExecutionId,
    Guid OrderId,
    Guid? PositionId,
    Guid TradingAccountId,
    string Symbol,
    string CompanyName,
    int Quantity,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal TotalInvestment,
    decimal TotalExitValue,
    decimal GrossPnL,
    decimal ReturnPercent,
    IndianBrokerageChargesDto Charges,
    decimal NetRealizedPnL,
    string HoldDuration,
    string ExitTrigger, // StopLoss, Target, StrategySignal, SquareOff, Manual
    DateTime EntryTimeUtc,
    DateTime ExecutedAtUtc
);

public record TradeHistoryDto(
    Guid ExecutionId,
    Guid OrderId,
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string CompanyName,
    string Side,
    string ProductType,
    int Quantity,
    decimal Price,
    decimal TotalValue,
    decimal? EntryPrice,
    decimal? RealizedPnL,
    decimal? Charges,
    string Exchange,
    DateTime ExecutedAtUtc
);

public record TradeHistoryFilterRequest(
    Guid? TradingAccountId = null,
    string? Symbol = null,
    string? Side = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 50
);
