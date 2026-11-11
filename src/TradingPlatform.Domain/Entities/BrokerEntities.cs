using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Domain.Entities;

public class BrokerProvider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public BrokerCode Code { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? WebSocketUrl { get; set; }
    public string? AuthorizationUrl { get; set; }
    public string? TokenUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CapabilitiesJson { get; set; }

    public ICollection<BrokerConnection> Connections { get; set; } = [];
}

public class BrokerConnection : BaseEntity, IAggregateRoot, IMustHaveTenant, ISoftDelete
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BrokerProviderId { get; set; }
    public BrokerProvider BrokerProvider { get; set; } = null!;

    public string DisplayName { get; set; } = string.Empty;
    public string EncryptedApiKey { get; set; } = string.Empty;
    public string EncryptedApiSecret { get; set; } = string.Empty;
    public string? EncryptedAccessToken { get; set; }
    public string? EncryptedRefreshToken { get; set; }
    public DateTime? TokenExpiresAtUtc { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TradingEnabled { get; set; }
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;
    public DateTime? LastAuthenticatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public ICollection<TradingAccount> TradingAccounts { get; set; } = [];
}

public class TradingAccount : BaseEntity, IAggregateRoot, IMustHaveTenant, ISoftDelete
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid BrokerConnectionId { get; set; }
    public BrokerConnection BrokerConnection { get; set; } = null!;

    public string AccountId { get; set; } = string.Empty; // Broker's account/client ID
    public string AccountName { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Equity;
    public bool IsActive { get; set; } = true;
    public bool TradingEnabled { get; set; }
    public TradingMode TradingMode { get; set; } = TradingMode.Paper;
    public decimal AvailableFunds { get; set; }
    public decimal AllocatedCapital { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public AccountConfiguration? Configuration { get; set; }
    public ICollection<RiskProfile> RiskProfiles { get; set; } = [];
    public ICollection<TradingOrder> Orders { get; set; } = [];
    public ICollection<Position> Positions { get; set; } = [];
}
