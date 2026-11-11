using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Contracts.Brokers;

public record BrokerProviderDto(
    Guid Id,
    string Name,
    BrokerCode Code,
    string ApiBaseUrl,
    bool IsActive
);

public record CreateBrokerConnectionRequest(
    Guid BrokerProviderId,
    string DisplayName,
    string ApiKey,
    string ApiSecret,
    TradingMode TradingMode = TradingMode.Paper
);

public record BrokerConnectionDto(
    Guid Id,
    Guid BrokerProviderId,
    string ProviderName,
    BrokerCode ProviderCode,
    string DisplayName,
    bool IsAuthenticated,
    bool IsActive,
    bool TradingEnabled,
    TradingMode TradingMode,
    DateTime? TokenExpiresAtUtc,
    string MaskedApiKey,
    DateTime CreatedAtUtc
);

public record GetAuthUrlRequest(
    string RedirectUri,
    string? State = null
);

public record AuthUrlResponse(
    string AuthorizationUrl
);

public record ExchangeTokenRequest(
    string AuthorizationCode,
    string RedirectUri
);

public record ToggleTradingModeRequest(
    TradingMode TradingMode
);

public record TradingAccountDto(
    Guid Id,
    Guid BrokerConnectionId,
    string AccountId,
    string AccountName,
    AccountType AccountType,
    bool IsActive,
    bool TradingEnabled,
    TradingMode TradingMode,
    decimal AvailableFunds,
    decimal AllocatedCapital,
    AccountConfigurationDto? Configuration
);

public record AccountConfigurationDto(
    decimal MaxCapitalAllocation,
    decimal MaxOrderValue,
    int MaxPositions,
    decimal MaxRiskPerTradePercent,
    decimal DailyLossLimit,
    decimal MaxExposurePerStockPercent,
    int MaxTradesPerDay,
    bool StopLossRequired,
    int ApprovalDurationSeconds,
    bool AutoExecuteOnTimeout,
    bool KillSwitchActive,
    TimeOnly TradingScheduleStart,
    TimeOnly TradingScheduleEnd,
    TimeOnly IntradaySquareOffTime
);

public record UpdateAccountConfigurationRequest(
    decimal MaxCapitalAllocation,
    decimal MaxOrderValue,
    int MaxPositions,
    decimal MaxRiskPerTradePercent,
    decimal DailyLossLimit,
    decimal MaxExposurePerStockPercent,
    int MaxTradesPerDay,
    bool StopLossRequired,
    int ApprovalDurationSeconds,
    bool AutoExecuteOnTimeout,
    bool KillSwitchActive
);
