using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Contracts.Brokers;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Application.Features.Brokers;

#region Queries
public record GetBrokerProvidersQuery : IRequest<IReadOnlyList<BrokerProviderDto>>;

public class GetBrokerProvidersHandler(ITradingDbContext dbContext) : IRequestHandler<GetBrokerProvidersQuery, IReadOnlyList<BrokerProviderDto>>
{
    public async Task<IReadOnlyList<BrokerProviderDto>> Handle(GetBrokerProvidersQuery request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.BrokerProviders
            .Where(bp => bp.IsActive)
            .OrderBy(bp => bp.Name)
            .Select(bp => new BrokerProviderDto(bp.Id, bp.Name, bp.Code, bp.ApiBaseUrl, bp.IsActive))
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            return existing;
        }

        // Auto-seed default providers if running on fresh or in-memory database
        var defaultProviders = new List<BrokerProvider>
        {
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Mock Simulator",
                Code = BrokerCode.Mock,
                ApiBaseUrl = "http://localhost:5000/mock-broker",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Name = "Upstox",
                Code = BrokerCode.Upstox,
                ApiBaseUrl = "https://api.upstox.com/v2",
                WebSocketUrl = "wss://ws-feed.upstox.com/v2/feed",
                AuthorizationUrl = "https://api.upstox.com/v2/login/authorization/dialog",
                TokenUrl = "https://api.upstox.com/v2/login/authorization/token",
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Name = "Zerodha",
                Code = BrokerCode.Zerodha,
                ApiBaseUrl = "https://api.kite.trade",
                WebSocketUrl = "wss://ws.kite.trade",
                AuthorizationUrl = "https://kite.zerodha.com/connect/login",
                TokenUrl = "https://api.kite.trade/session/token",
                IsActive = true
            }
        };

        dbContext.BrokerProviders.AddRange(defaultProviders);
        await dbContext.SaveChangesAsync(cancellationToken);

        return defaultProviders
            .Select(bp => new BrokerProviderDto(bp.Id, bp.Name, bp.Code, bp.ApiBaseUrl, bp.IsActive))
            .OrderBy(bp => bp.Name)
            .ToList();
    }
}

public record GetBrokerConnectionsQuery : IRequest<IReadOnlyList<BrokerConnectionDto>>;

public class GetBrokerConnectionsHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<GetBrokerConnectionsQuery, IReadOnlyList<BrokerConnectionDto>>
{
    public async Task<IReadOnlyList<BrokerConnectionDto>> Handle(GetBrokerConnectionsQuery request, CancellationToken cancellationToken)
    {
        var connections = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .OrderByDescending(bc => bc.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return connections.Select(bc =>
        {
            string maskedApiKey = "******";
            try
            {
                var decrypted = encryptionService.Decrypt(bc.EncryptedApiKey);
                if (decrypted.Length > 4)
                {
                    maskedApiKey = $"***{decrypted[^4..]}";
                }
            }
            catch
            {
                maskedApiKey = "******";
            }

            return new BrokerConnectionDto(
                bc.Id,
                bc.BrokerProviderId,
                bc.BrokerProvider.Name,
                bc.BrokerProvider.Code,
                bc.DisplayName,
                bc.IsAuthenticated,
                bc.IsActive,
                bc.TradingEnabled,
                bc.TradingMode,
                bc.TokenExpiresAtUtc,
                maskedApiKey,
                bc.CreatedAtUtc
            );
        }).ToList();
    }
}
#endregion

#region Commands
public record CreateBrokerConnectionCommand(CreateBrokerConnectionRequest Request) : IRequest<BrokerConnectionDto>;

public class CreateBrokerConnectionValidator : AbstractValidator<CreateBrokerConnectionCommand>
{
    public CreateBrokerConnectionValidator()
    {
        RuleFor(x => x.Request.BrokerProviderId).NotEmpty().WithMessage("Broker provider is required.");
        RuleFor(x => x.Request.DisplayName).NotEmpty().MaximumLength(64).WithMessage("Display name is required.");
        RuleFor(x => x.Request.ApiKey).NotEmpty().WithMessage("API Key is required.");
        RuleFor(x => x.Request.ApiSecret).NotEmpty().WithMessage("API Secret is required.");
    }
}

public class CreateBrokerConnectionHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<CreateBrokerConnectionCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(CreateBrokerConnectionCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var provider = await dbContext.BrokerProviders.FindAsync([req.BrokerProviderId], cancellationToken)
            ?? throw new KeyNotFoundException("Selected broker provider does not exist.");

        // Encrypt credentials at rest using AES-256-GCM
        var encryptedApiKey = encryptionService.Encrypt(req.ApiKey.Trim());
        var encryptedApiSecret = encryptionService.Encrypt(req.ApiSecret.Trim());

        var isMock = provider.Code == BrokerCode.Mock;
        var isPaper = req.TradingMode == TradingMode.Paper;
        var isReady = isMock || isPaper;

        var connection = new BrokerConnection
        {
            BrokerProviderId = provider.Id,
            DisplayName = req.DisplayName.Trim(),
            EncryptedApiKey = encryptedApiKey,
            EncryptedApiSecret = encryptedApiSecret,
            TradingMode = req.TradingMode,
            IsActive = true,
            TradingEnabled = true,
            IsAuthenticated = isReady, // Mock and Paper modes are immediately authenticated
            LastAuthenticatedAtUtc = isReady ? DateTime.UtcNow : null,
            TokenExpiresAtUtc = isReady ? DateTime.UtcNow.AddYears(1) : null,
            EncryptedAccessToken = isReady ? encryptionService.Encrypt($"simulated_paper_token_{Guid.NewGuid():N}") : null
        };

        // Initialize associated TradingAccount
        var defaultAccount = new TradingAccount
        {
            BrokerConnection = connection,
            AccountId = isReady ? $"{provider.Code.ToString().ToUpperInvariant()}-PAPER-{Guid.NewGuid():N}"[..14] : "PENDING_AUTH",
            AccountName = $"{req.DisplayName} Primary",
            AccountType = AccountType.Equity,
            IsActive = true,
            TradingEnabled = true,
            TradingMode = req.TradingMode,
            AvailableFunds = isReady ? (req.InitialCapital > 0 ? req.InitialCapital : 500_000m) : 0m,
            AllocatedCapital = 100_000m,
            Configuration = new AccountConfiguration
            {
                MaxCapitalAllocation = 100_000m,
                MaxOrderValue = 25_000m,
                MaxPositions = 5,
                MaxRiskPerTradePercent = 1.0m,
                DailyLossLimit = 5_000m,
                MaxExposurePerStockPercent = 20.0m,
                MaxTradesPerDay = 10,
                StopLossRequired = true,
                ApprovalDurationSeconds = 2,
                AutoExecuteOnTimeout = req.TradingMode == TradingMode.Paper, // True for paper, false for live
                KillSwitchActive = false
            }
        };

        connection.TradingAccounts.Add(defaultAccount);
        dbContext.BrokerConnections.Add(connection);
        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = req.ApiKey.Length > 4 ? $"***{req.ApiKey[^4..]}" : "******";

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            provider.Name,
            provider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record GetBrokerAuthUrlQuery(Guid ConnectionId, string RedirectUri, string? State) : IRequest<AuthUrlResponse>;

public class GetBrokerAuthUrlHandler(
    ITradingDbContext dbContext,
    IBrokerAdapterFactory adapterFactory) : IRequestHandler<GetBrokerAuthUrlQuery, AuthUrlResponse>
{
    public async Task<AuthUrlResponse> Handle(GetBrokerAuthUrlQuery request, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .FirstOrDefaultAsync(bc => bc.Id == request.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        var adapter = adapterFactory.GetAdapter(connection.BrokerProvider.Code);
        var url = await adapter.Authentication.GetAuthorizationUrlAsync(
            connection,
            request.RedirectUri,
            request.State ?? Guid.NewGuid().ToString("N"));

        return new AuthUrlResponse(url);
    }
}

public record ExchangeBrokerTokenCommand(Guid ConnectionId, ExchangeTokenRequest Request) : IRequest<BrokerConnectionDto>;

public class ExchangeBrokerTokenHandler(
    ITradingDbContext dbContext,
    IBrokerAdapterFactory adapterFactory,
    ICredentialEncryptionService encryptionService) : IRequestHandler<ExchangeBrokerTokenCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(ExchangeBrokerTokenCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        var adapter = adapterFactory.GetAdapter(connection.BrokerProvider.Code);
        var tokenResult = await adapter.Authentication.ExchangeCodeAsync(
            connection,
            command.Request.AuthorizationCode,
            command.Request.RedirectUri);

        if (!tokenResult.Success)
        {
            throw new InvalidOperationException($"OAuth token exchange failed: {tokenResult.ErrorMessage}");
        }

        // Encrypt tokens before persistence
        connection.EncryptedAccessToken = encryptionService.Encrypt(tokenResult.AccessToken);
        if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
        {
            connection.EncryptedRefreshToken = encryptionService.Encrypt(tokenResult.RefreshToken);
        }

        connection.TokenExpiresAtUtc = tokenResult.ExpiresAtUtc;
        connection.IsAuthenticated = true;
        connection.LastAuthenticatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = "******";
        try
        {
            var raw = encryptionService.Decrypt(connection.EncryptedApiKey);
            if (raw.Length > 4) maskedApiKey = $"***{raw[^4..]}";
        }
        catch { }

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            connection.BrokerProvider.Name,
            connection.BrokerProvider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record ToggleTradingModeCommand(Guid ConnectionId, TradingMode NewMode) : IRequest<BrokerConnectionDto>;

public class ToggleTradingModeHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<ToggleTradingModeCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(ToggleTradingModeCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .Include(bc => bc.TradingAccounts)
                .ThenInclude(ta => ta.Configuration)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        if (connection.BrokerProvider.Code == BrokerCode.Mock && command.NewMode == TradingMode.Live)
        {
            throw new InvalidOperationException("Mock Simulator cannot be switched to Live Trading mode.");
        }

        connection.TradingMode = command.NewMode;

        foreach (var account in connection.TradingAccounts)
        {
            account.TradingMode = command.NewMode;
            if (account.Configuration != null)
            {
                // Live mode disables auto-execute by default for safety
                if (command.NewMode == TradingMode.Live)
                {
                    account.Configuration.AutoExecuteOnTimeout = false;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = "******";
        try
        {
            var raw = encryptionService.Decrypt(connection.EncryptedApiKey);
            if (raw.Length > 4) maskedApiKey = $"***{raw[^4..]}";
        }
        catch { }

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            connection.BrokerProvider.Name,
            connection.BrokerProvider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record ToggleBrokerActiveCommand(Guid ConnectionId) : IRequest<BrokerConnectionDto>;

public class ToggleBrokerActiveHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<ToggleBrokerActiveCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(ToggleBrokerActiveCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .Include(bc => bc.TradingAccounts)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        connection.IsActive = !connection.IsActive;
        connection.TradingEnabled = connection.IsActive;

        foreach (var account in connection.TradingAccounts)
        {
            account.IsActive = connection.IsActive;
            account.TradingEnabled = connection.IsActive;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = "******";
        try
        {
            var raw = encryptionService.Decrypt(connection.EncryptedApiKey);
            if (raw.Length > 4) maskedApiKey = $"***{raw[^4..]}";
        }
        catch { }

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            connection.BrokerProvider.Name,
            connection.BrokerProvider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record ActivatePaperConnectionCommand(Guid ConnectionId) : IRequest<BrokerConnectionDto>;

public class ActivatePaperConnectionHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<ActivatePaperConnectionCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(ActivatePaperConnectionCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .Include(bc => bc.TradingAccounts)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        connection.IsAuthenticated = true;
        connection.LastAuthenticatedAtUtc = DateTime.UtcNow;
        connection.TokenExpiresAtUtc = DateTime.UtcNow.AddYears(1);
        connection.EncryptedAccessToken = encryptionService.Encrypt($"simulated_paper_token_{Guid.NewGuid():N}");

        foreach (var account in connection.TradingAccounts)
        {
            if (account.AccountId == "PENDING_AUTH" || string.IsNullOrWhiteSpace(account.AccountId))
            {
                account.AccountId = $"{connection.BrokerProvider.Code.ToString().ToUpperInvariant()}-PAPER-{Guid.NewGuid():N}"[..14];
            }
            if (account.AvailableFunds <= 0)
            {
                account.AvailableFunds = 500_000m;
            }
            account.TradingEnabled = true;
            account.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = "******";
        try
        {
            var raw = encryptionService.Decrypt(connection.EncryptedApiKey);
            if (raw.Length > 4) maskedApiKey = $"***{raw[^4..]}";
        }
        catch { }

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            connection.BrokerProvider.Name,
            connection.BrokerProvider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record SetManualTokenCommand(Guid ConnectionId, string AccessToken, DateTime? ExpiresAtUtc = null) : IRequest<BrokerConnectionDto>;

public class SetManualTokenHandler(
    ITradingDbContext dbContext,
    ICredentialEncryptionService encryptionService) : IRequestHandler<SetManualTokenCommand, BrokerConnectionDto>
{
    public async Task<BrokerConnectionDto> Handle(SetManualTokenCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.BrokerProvider)
            .Include(bc => bc.TradingAccounts)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException("Broker connection not found.");

        connection.IsAuthenticated = true;
        connection.LastAuthenticatedAtUtc = DateTime.UtcNow;
        connection.TokenExpiresAtUtc = command.ExpiresAtUtc ?? DateTime.UtcNow.AddHours(24);
        connection.EncryptedAccessToken = encryptionService.Encrypt(command.AccessToken.Trim());

        foreach (var account in connection.TradingAccounts)
        {
            if (account.AccountId == "PENDING_AUTH" || string.IsNullOrWhiteSpace(account.AccountId))
            {
                account.AccountId = $"{connection.BrokerProvider.Code.ToString().ToUpperInvariant()}-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
            }
            account.TradingEnabled = true;
            account.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        string maskedApiKey = "******";
        try
        {
            var raw = encryptionService.Decrypt(connection.EncryptedApiKey);
            if (raw.Length > 4) maskedApiKey = $"***{raw[^4..]}";
        }
        catch { }

        return new BrokerConnectionDto(
            connection.Id,
            connection.BrokerProviderId,
            connection.BrokerProvider.Name,
            connection.BrokerProvider.Code,
            connection.DisplayName,
            connection.IsAuthenticated,
            connection.IsActive,
            connection.TradingEnabled,
            connection.TradingMode,
            connection.TokenExpiresAtUtc,
            maskedApiKey,
            connection.CreatedAtUtc
        );
    }
}

public record DeleteBrokerConnectionCommand(Guid ConnectionId) : IRequest<bool>;

public class DeleteBrokerConnectionHandler(ITradingDbContext dbContext) : IRequestHandler<DeleteBrokerConnectionCommand, bool>
{
    public async Task<bool> Handle(DeleteBrokerConnectionCommand command, CancellationToken cancellationToken)
    {
        var connection = await dbContext.BrokerConnections
            .Include(bc => bc.TradingAccounts)
            .FirstOrDefaultAsync(bc => bc.Id == command.ConnectionId, cancellationToken);

        if (connection == null) return false;

        connection.IsActive = false;
        connection.TradingEnabled = false;
        connection.IsDeleted = true;
        connection.DeletedAtUtc = DateTime.UtcNow;

        foreach (var acc in connection.TradingAccounts)
        {
            acc.IsActive = false;
            acc.TradingEnabled = false;
            acc.IsDeleted = true;
            acc.DeletedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
#endregion

#region Trading Accounts
public record GetTradingAccountsQuery : IRequest<IReadOnlyList<TradingAccountDto>>;

public class GetTradingAccountsHandler(
    ITradingDbContext dbContext,
    IBrokerAdapterFactory adapterFactory) : IRequestHandler<GetTradingAccountsQuery, IReadOnlyList<TradingAccountDto>>
{
    public async Task<IReadOnlyList<TradingAccountDto>> Handle(GetTradingAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await dbContext.TradingAccounts
            .Include(ta => ta.BrokerConnection)
                .ThenInclude(bc => bc.BrokerProvider)
            .Include(ta => ta.Configuration)
            .Where(ta => ta.IsActive && ta.BrokerConnection.IsActive)
            .OrderBy(ta => ta.AccountName)
            .ToListAsync(cancellationToken);

        var list = new List<TradingAccountDto>();
        foreach (var acc in accounts)
        {
            decimal availableFunds = acc.AvailableFunds;

            // Fetch live funds from adapter if connection is active
            if (acc.BrokerConnection.IsAuthenticated && acc.BrokerConnection.IsActive)
            {
                try
                {
                    var adapter = adapterFactory.GetAdapter(acc.BrokerConnection.BrokerProvider.Code);
                    var liveFunds = await adapter.Funds.GetFundsAsync(acc.BrokerConnection, acc, cancellationToken);
                    availableFunds = liveFunds.AvailableCash;
                    acc.AvailableFunds = availableFunds;
                }
                catch
                {
                    // Fall back to persisted funds on error
                }
            }

            AccountConfigurationDto? configDto = null;
            if (acc.Configuration != null)
            {
                var c = acc.Configuration;
                configDto = new AccountConfigurationDto(
                    c.MaxCapitalAllocation,
                    c.MaxOrderValue,
                    c.MaxPositions,
                    c.MaxRiskPerTradePercent,
                    c.DailyLossLimit,
                    c.MaxExposurePerStockPercent,
                    c.MaxTradesPerDay,
                    c.StopLossRequired,
                    c.ApprovalDurationSeconds,
                    c.AutoExecuteOnTimeout,
                    c.KillSwitchActive,
                    c.TradingScheduleStart,
                    c.TradingScheduleEnd,
                    c.IntradaySquareOffTime,
                    c.AllowedTradingSegment ?? "Both",
                    c.IntradayMinVolume,
                    c.IntradayStopLossPercent,
                    c.IntradayTargetPercent,
                    c.IntradayMaxCapitalAllocation,
                    c.IntradayMaxOrderValue,
                    c.IntradayDailyLossLimit,
                    c.IntradayMaxPositions,
                    c.DeliveryMinVolume,
                    c.DeliveryStopLossPercent,
                    c.DeliveryTargetPercent,
                    c.DeliveryMaxCapitalAllocation,
                    c.DeliveryMaxOrderValue,
                    c.DeliveryDailyLossLimit,
                    c.DeliveryMaxPositions
                );
            }

            list.Add(new TradingAccountDto(
                acc.Id,
                acc.BrokerConnectionId,
                acc.AccountId,
                acc.AccountName,
                acc.AccountType,
                acc.IsActive,
                acc.TradingEnabled,
                acc.TradingMode,
                availableFunds,
                acc.AllocatedCapital,
                configDto
            ));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return list;
    }
}

public record UpdateAccountConfigurationCommand(Guid AccountId, UpdateAccountConfigurationRequest Request) : IRequest<AccountConfigurationDto>;

public class UpdateAccountConfigurationValidator : AbstractValidator<UpdateAccountConfigurationCommand>
{
    public UpdateAccountConfigurationValidator()
    {
        RuleFor(x => x.Request.MaxCapitalAllocation).GreaterThan(0).WithMessage("Max capital allocation must be positive.");
        RuleFor(x => x.Request.MaxOrderValue).GreaterThan(0).WithMessage("Max order value must be positive.");
        RuleFor(x => x.Request.MaxPositions).GreaterThan(0).WithMessage("Max positions must be greater than zero.");
        RuleFor(x => x.Request.DailyLossLimit).GreaterThan(0).WithMessage("Daily loss limit must be positive.");
        RuleFor(x => x.Request.ApprovalDurationSeconds).InclusiveBetween(1, 30).WithMessage("Approval countdown must be between 1 and 30 seconds.");
    }
}

public class UpdateAccountConfigurationHandler(ITradingDbContext dbContext) : IRequestHandler<UpdateAccountConfigurationCommand, AccountConfigurationDto>
{
    public async Task<AccountConfigurationDto> Handle(UpdateAccountConfigurationCommand command, CancellationToken cancellationToken)
    {
        var account = await dbContext.TradingAccounts
            .Include(ta => ta.Configuration)
            .FirstOrDefaultAsync(ta => ta.Id == command.AccountId, cancellationToken)
            ?? throw new KeyNotFoundException("Trading account not found.");

        var c = account.Configuration ?? new AccountConfiguration { TradingAccountId = account.Id, UserId = account.UserId };
        var req = command.Request;

        c.MaxCapitalAllocation = req.MaxCapitalAllocation;
        c.MaxOrderValue = req.MaxOrderValue;
        c.MaxPositions = req.MaxPositions;
        c.MaxRiskPerTradePercent = req.MaxRiskPerTradePercent;
        c.DailyLossLimit = req.DailyLossLimit;
        c.MaxExposurePerStockPercent = req.MaxExposurePerStockPercent;
        c.MaxTradesPerDay = req.MaxTradesPerDay;
        c.StopLossRequired = req.StopLossRequired;
        c.ApprovalDurationSeconds = req.ApprovalDurationSeconds;
        c.AutoExecuteOnTimeout = req.AutoExecuteOnTimeout;
        c.KillSwitchActive = req.KillSwitchActive;

        if (req.AllowedTradingSegment != null) c.AllowedTradingSegment = req.AllowedTradingSegment;
        if (req.IntradayMinVolume.HasValue) c.IntradayMinVolume = req.IntradayMinVolume.Value;
        if (req.IntradayStopLossPercent.HasValue) c.IntradayStopLossPercent = req.IntradayStopLossPercent.Value;
        if (req.IntradayTargetPercent.HasValue) c.IntradayTargetPercent = req.IntradayTargetPercent.Value;
        if (req.IntradayMaxCapitalAllocation.HasValue) c.IntradayMaxCapitalAllocation = req.IntradayMaxCapitalAllocation.Value;
        if (req.IntradayMaxOrderValue.HasValue) c.IntradayMaxOrderValue = req.IntradayMaxOrderValue.Value;
        if (req.IntradayDailyLossLimit.HasValue) c.IntradayDailyLossLimit = req.IntradayDailyLossLimit.Value;
        if (req.IntradayMaxPositions.HasValue) c.IntradayMaxPositions = req.IntradayMaxPositions.Value;

        if (req.DeliveryMinVolume.HasValue) c.DeliveryMinVolume = req.DeliveryMinVolume.Value;
        if (req.DeliveryStopLossPercent.HasValue) c.DeliveryStopLossPercent = req.DeliveryStopLossPercent.Value;
        if (req.DeliveryTargetPercent.HasValue) c.DeliveryTargetPercent = req.DeliveryTargetPercent.Value;
        if (req.DeliveryMaxCapitalAllocation.HasValue) c.DeliveryMaxCapitalAllocation = req.DeliveryMaxCapitalAllocation.Value;
        if (req.DeliveryMaxOrderValue.HasValue) c.DeliveryMaxOrderValue = req.DeliveryMaxOrderValue.Value;
        if (req.DeliveryDailyLossLimit.HasValue) c.DeliveryDailyLossLimit = req.DeliveryDailyLossLimit.Value;
        if (req.DeliveryMaxPositions.HasValue) c.DeliveryMaxPositions = req.DeliveryMaxPositions.Value;

        if (account.Configuration == null)
        {
            dbContext.AccountConfigurations.Add(c);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AccountConfigurationDto(
            c.MaxCapitalAllocation,
            c.MaxOrderValue,
            c.MaxPositions,
            c.MaxRiskPerTradePercent,
            c.DailyLossLimit,
            c.MaxExposurePerStockPercent,
            c.MaxTradesPerDay,
            c.StopLossRequired,
            c.ApprovalDurationSeconds,
            c.AutoExecuteOnTimeout,
            c.KillSwitchActive,
            c.TradingScheduleStart,
            c.TradingScheduleEnd,
            c.IntradaySquareOffTime,
            c.AllowedTradingSegment,
            c.IntradayMinVolume,
            c.IntradayStopLossPercent,
            c.IntradayTargetPercent,
            c.IntradayMaxCapitalAllocation,
            c.IntradayMaxOrderValue,
            c.IntradayDailyLossLimit,
            c.IntradayMaxPositions,
            c.DeliveryMinVolume,
            c.DeliveryStopLossPercent,
            c.DeliveryTargetPercent,
            c.DeliveryMaxCapitalAllocation,
            c.DeliveryMaxOrderValue,
            c.DeliveryDailyLossLimit,
            c.DeliveryMaxPositions
        );
    }
}
#endregion
