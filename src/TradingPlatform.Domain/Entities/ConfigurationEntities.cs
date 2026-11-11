using TradingPlatform.Domain.Common;

namespace TradingPlatform.Domain.Entities;

public class AccountConfiguration : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;

    public decimal MaxCapitalAllocation { get; set; } = 100000m;
    public decimal MaxOrderValue { get; set; } = 25000m;
    public int MaxPositions { get; set; } = 5;
    public decimal MaxRiskPerTradePercent { get; set; } = 1.0m;
    public decimal DailyLossLimit { get; set; } = 5000m;
    public decimal MaxExposurePerStockPercent { get; set; } = 20.0m;
    public int MaxTradesPerDay { get; set; } = 10;
    public bool StopLossRequired { get; set; } = true;
    public int ApprovalDurationSeconds { get; set; } = 2;
    public bool AutoExecuteOnTimeout { get; set; } // default false for live accounts
    public bool KillSwitchActive { get; set; }
    public TimeOnly TradingScheduleStart { get; set; } = new(9, 15);
    public TimeOnly TradingScheduleEnd { get; set; } = new(15, 30);
    public TimeOnly IntradaySquareOffTime { get; set; } = new(15, 15);
}

public class RiskProfile : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;

    public string ProfileName { get; set; } = "Conservative";
    public decimal MaxCapital { get; set; }
    public decimal MaxDailyLoss { get; set; }
    public decimal MaxPositionSize { get; set; }
    public bool IsActive { get; set; } = true;
}
