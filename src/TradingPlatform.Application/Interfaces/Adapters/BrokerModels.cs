using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Application.Interfaces.Adapters;

public record BrokerCapabilities(
    bool SupportsWebSocket,
    bool SupportsHistoricalData,
    bool SupportsGttOrders,
    bool SupportsBracketOrders,
    IReadOnlyList<string> SupportedExchanges,
    IReadOnlyList<ProductType> SupportedProductTypes,
    IReadOnlyList<OrderType> SupportedOrderTypes,
    int MaxOrdersPerSecond
);

public record Quote(
    string InstrumentToken,
    string TradingSymbol,
    string Exchange,
    decimal LastPrice,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Change,
    decimal ChangePercent,
    long Volume,
    decimal? BidPrice,
    decimal? AskPrice,
    DateTime TimestampUtc
);

public record HistoricalCandle(
    DateTime TimestampUtc,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);

public record OrderRequest(
    string InstrumentToken,
    string TradingSymbol,
    string Exchange,
    OrderSide Side,
    OrderType OrderType,
    ProductType ProductType,
    int Quantity,
    decimal Price,
    decimal? TriggerPrice,
    Guid IdempotencyKey
);

public record OrderResponse(
    bool Success,
    string? BrokerOrderId,
    OrderStatus Status,
    string? Message,
    string? ErrorCode = null
);

public record OrderDetails(
    string BrokerOrderId,
    string TradingSymbol,
    OrderSide Side,
    OrderType OrderType,
    ProductType ProductType,
    int Quantity,
    int FilledQuantity,
    decimal Price,
    decimal AveragePrice,
    OrderStatus Status,
    string? StatusMessage,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record PositionDetails(
    string InstrumentToken,
    string TradingSymbol,
    ProductType ProductType,
    int Quantity,
    decimal AverageBuyPrice,
    decimal CurrentPrice,
    decimal RealizedPnL,
    decimal UnrealizedPnL
);

public record FundsDetails(
    decimal AvailableCash,
    decimal UsedMargin,
    decimal TotalCapital,
    decimal CollateralMargin
);

public record TokenExchangeResult(
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAtUtc,
    bool Success,
    string? ErrorMessage = null
);
