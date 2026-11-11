namespace TradingPlatform.Contracts.Orders;

public record IndianBrokerageChargesDto(
    decimal Brokerage,
    decimal Stt,
    decimal ExchangeCharges,
    decimal Gst,
    decimal SebiFees,
    decimal StampDuty,
    decimal DpCharges,
    decimal TotalCharges
);

public record OrderEventDto(
    Guid Id,
    Guid OrderId,
    string PreviousStatus,
    string NewStatus,
    string EventType,
    string? Message,
    DateTime CreatedAtUtc
);

public record TradeExecutionDto(
    Guid Id,
    Guid OrderId,
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    decimal TotalValue,
    string Exchange,
    DateTime ExecutedAtUtc
);

public record OrderDto(
    Guid Id,
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string CompanyName,
    string Side,
    string OrderType,
    string ProductType,
    int Quantity,
    int FilledQuantity,
    decimal Price,
    decimal? TriggerPrice,
    decimal AveragePrice,
    string Status,
    string? StatusMessage,
    string? BrokerOrderId,
    Guid IdempotencyKey,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<OrderEventDto> Events,
    IReadOnlyList<TradeExecutionDto> Executions
);

public record CreateOrderRequest(
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string Side,
    string OrderType,
    string ProductType,
    int Quantity,
    decimal Price,
    decimal? TriggerPrice = null,
    decimal? StopLoss = null,
    decimal? Target = null,
    Guid? StrategyVersionId = null,
    Guid? PendingTradeApprovalId = null
);

public record CancelOrderRequest(
    Guid OrderId,
    string? Reason = null
);

public record OrderFilterRequest(
    Guid? TradingAccountId = null,
    string? Symbol = null,
    string? Status = null,
    string? Side = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int PageNumber = 1,
    int PageSize = 50
);

public record PositionDto(
    Guid Id,
    Guid TradingAccountId,
    Guid InstrumentId,
    string Symbol,
    string CompanyName,
    string Side,
    string ProductType,
    int Quantity,
    decimal AverageBuyPrice,
    decimal CurrentPrice,
    decimal? StopLoss,
    decimal? Target,
    decimal UnrealizedPnL,
    decimal RealizedGrossPnL,
    decimal RealizedNetPnL,
    decimal TotalCharges,
    bool IsOpen,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc,
    string? ExitReason
);

public record PortfolioSummaryDto(
    Guid TradingAccountId,
    int OpenPositionsCount,
    decimal TotalCurrentValue,
    decimal TotalInvestmentValue,
    decimal TotalUnrealizedPnL,
    decimal TotalRealizedGrossPnL,
    decimal TotalRealizedNetPnL,
    decimal TotalChargesPaid,
    decimal AvailableFunds,
    IReadOnlyList<PositionDto> Positions
);

public record ClosePositionRequest(
    Guid PositionId,
    string? Reason = "Manual Exit"
);
