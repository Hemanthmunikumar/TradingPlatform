using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Domain.Entities;

public class StrategyDefinition : BaseEntity, IAggregateRoot, IMustHaveTenant, ISoftDelete
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StrategyType StrategyType { get; set; } = StrategyType.Intraday;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<StrategyVersion> Versions { get; set; } = [];
}

public class StrategyVersion : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid StrategyDefinitionId { get; set; }
    public StrategyDefinition StrategyDefinition { get; set; } = null!;

    public int VersionNumber { get; set; } = 1;
    public string ConfigurationJson { get; set; } = "{}"; // Indicators, parameters, entry/exit rules
    public string? StockUniverseJson { get; set; }
    public decimal CapitalAllocation { get; set; }
    public int MaxPositions { get; set; } = 3;
    public decimal RiskPerTradePercent { get; set; } = 1.0m;
    public decimal StopLossPercent { get; set; } = 1.0m;
    public decimal TargetPercent { get; set; } = 2.0m;
    public bool IsActive { get; set; } = true;
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? DeactivatedAtUtc { get; set; }

    public ICollection<StrategyAccountMapping> AccountMappings { get; set; } = [];
}

public class StrategyAccountMapping : BaseEntity, IMustHaveTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid StrategyVersionId { get; set; }
    public StrategyVersion StrategyVersion { get; set; } = null!;

    public Guid TradingAccountId { get; set; }
    public TradingAccount TradingAccount { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
