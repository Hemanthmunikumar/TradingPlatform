namespace TradingPlatform.Domain.Enums;

public enum OrderSide
{
    Buy = 1,
    Sell = 2
}

public enum OrderType
{
    Market = 1,
    Limit = 2,
    StopLoss = 3,
    StopLossMarket = 4
}

public enum ProductType
{
    CNC = 1,  // Cash and Carry (Delivery)
    MIS = 2,  // Margin Intraday Square-off (Intraday)
    NRML = 3  // Normal (F&O / Commodity Carry-forward)
}

public enum OrderStatus
{
    Created = 1,
    Submitted = 2,
    Accepted = 3,
    Open = 4,
    PartiallyFilled = 5,
    Filled = 6,
    Cancelled = 7,
    Rejected = 8,
    Error = 9
}

public enum ApprovalStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

public enum SignalType
{
    Buy = 1,
    Sell = 2,
    Hold = 3
}

public enum SignalStatus
{
    Generated = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Expired = 5,
    Executed = 6
}

public enum TradingMode
{
    Paper = 1,
    Live = 2
}

public enum BrokerCode
{
    Mock = 1,
    Upstox = 2,
    Zerodha = 3,
    Dhan = 4,
    AngelOne = 5
}

public enum AccountType
{
    Equity = 1,
    Commodity = 2,
    Derivatives = 3
}

public enum StrategyType
{
    Intraday = 1,
    Swing = 2
}

public enum NotificationChannel
{
    SignalR = 1,
    Email = 2,
    WhatsApp = 3
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4
}
