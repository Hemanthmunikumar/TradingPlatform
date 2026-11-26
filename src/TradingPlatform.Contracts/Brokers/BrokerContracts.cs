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
    TradingMode TradingMode = TradingMode.Paper,
    decimal InitialCapital = 500_000m
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

public record SetManualTokenRequest(
    string AccessToken,
    DateTime? ExpiresAtUtc = null
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
    TimeOnly IntradaySquareOffTime,
    string AllowedTradingSegment = "Both",
    int IntradayMinVolume = 30000,
    decimal IntradayStopLossPercent = 2.0m,
    decimal IntradayTargetPercent = 5.0m,
    decimal IntradayMaxCapitalAllocation = 500000m,
    decimal IntradayMaxOrderValue = 50000m,
    decimal IntradayDailyLossLimit = 10000m,
    int IntradayMaxPositions = 5,
    int DeliveryMinVolume = 50000,
    decimal DeliveryStopLossPercent = 4.0m,
    decimal DeliveryTargetPercent = 10.0m,
    decimal DeliveryMaxCapitalAllocation = 500000m,
    decimal DeliveryMaxOrderValue = 100000m,
    decimal DeliveryDailyLossLimit = 20000m,
    int DeliveryMaxPositions = 10
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
    bool KillSwitchActive,
    string? AllowedTradingSegment = null,
    int? IntradayMinVolume = null,
    decimal? IntradayStopLossPercent = null,
    decimal? IntradayTargetPercent = null,
    decimal? IntradayMaxCapitalAllocation = null,
    decimal? IntradayMaxOrderValue = null,
    decimal? IntradayDailyLossLimit = null,
    int? IntradayMaxPositions = null,
    int? DeliveryMinVolume = null,
    decimal? DeliveryStopLossPercent = null,
    decimal? DeliveryTargetPercent = null,
    decimal? DeliveryMaxCapitalAllocation = null,
    decimal? DeliveryMaxOrderValue = null,
    decimal? DeliveryDailyLossLimit = null,
    int? DeliveryMaxPositions = null
);
