using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Domain.Entities;

public class TradingSignal : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;

    public Guid StrategyVersionId { get; set; }
    public StrategyVersion StrategyVersion { get; set; } = null!;

    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public SignalType SignalType { get; set; } = SignalType.Buy;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? Target { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Confidence { get; set; } = 1.0m;
    public string? IndicatorValuesJson { get; set; }
    public SignalStatus Status { get; set; } = SignalStatus.Generated;

    public PendingTradeApproval? PendingApproval { get; set; }
}

public class PendingTradeApproval : BaseEntity, IAggregateRoot, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;

    public Guid? TradingSignalId { get; set; }
    public TradingSignal? TradingSignal { get; set; }

    public string Symbol { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public OrderSide Side { get; set; } = OrderSide.Buy;
    public int Quantity { get; set; }
    public decimal IndicativePrice { get; set; }
    public decimal EstimatedOrderValue => Quantity * IndicativePrice;
    public decimal? StopLoss { get; set; }
    public decimal? Target { get; set; }
    public decimal? RiskAmount { get; set; }
    public string Reason { get; set; } = string.Empty;

    public DateTime ApprovalDeadlineUtc { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionReason { get; set; }

    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

    public bool IsExpired => Status == ApprovalStatus.Pending && DateTime.UtcNow >= ApprovalDeadlineUtc;
}

public class TradingOrder : BaseEntity, IAggregateRoot, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;

    public Guid? PendingTradeApprovalId { get; set; }
    public PendingTradeApproval? PendingTradeApproval { get; set; }

    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public Guid? StrategyVersionId { get; set; }
    public StrategyVersion? StrategyVersion { get; set; }

    public string? BrokerOrderId { get; set; } // External ID assigned by Upstox/Zerodha
    public OrderSide Side { get; set; } = OrderSide.Buy;
    public OrderType OrderType { get; set; } = OrderType.Market;
    public ProductType ProductType { get; set; } = ProductType.MIS;
    public int Quantity { get; set; }
    public int FilledQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal? TriggerPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public string? StatusMessage { get; set; }

    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    public DateTime? SubmittedAtUtc { get; set; }

    public ICollection<OrderEvent> Events { get; set; } = [];
    public ICollection<TradeExecution> Executions { get; set; } = [];
}

public class OrderEvent : BaseEntity
{
    public Guid TradingOrderId { get; set; }
    public TradingOrder TradingOrder { get; set; } = null!;

    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? BrokerResponseJson { get; set; }
}

public class TradeExecution : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingOrderId { get; set; }
    public TradingOrder TradingOrder { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;

    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public string? BrokerTradeId { get; set; }
    public OrderSide Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue => Quantity * Price;
    public string Exchange { get; set; } = "NSE";
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Position : BaseEntity, IAggregateRoot, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;

    public Guid InstrumentId { get; set; }
    public Instrument Instrument { get; set; } = null!;

    public Guid? StrategyVersionId { get; set; }
    public StrategyVersion? StrategyVersion { get; set; }

    public OrderSide Side { get; set; } = OrderSide.Buy;
    public ProductType ProductType { get; set; } = ProductType.MIS;
    public int Quantity { get; set; }
    public decimal AverageBuyPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal? StopLoss { get; set; }
    public decimal? Target { get; set; }

    public decimal RealizedGrossPnL { get; set; }
    public decimal RealizedNetPnL { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal UnrealizedPnL => Quantity > 0 ? (CurrentPrice - AverageBuyPrice) * Quantity : 0m;

    public bool IsOpen { get; set; } = true;
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ExitReason { get; set; }
}
