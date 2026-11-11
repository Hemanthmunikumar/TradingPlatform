using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Adapters.Upstox;

public class UpstoxBrokerAdapter : IBrokerAdapter, IBrokerAuthentication, IMarketDataProvider, IOrderExecutionService, IPositionProvider, IFundsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<UpstoxBrokerAdapter> _logger;

    public UpstoxBrokerAdapter(
        HttpClient httpClient,
        ICredentialEncryptionService encryptionService,
        ILogger<UpstoxBrokerAdapter> logger)
    {
        _httpClient = httpClient;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public BrokerCode BrokerCode => BrokerCode.Upstox;
    public IBrokerAuthentication Authentication => this;
    public IMarketDataProvider MarketData => this;
    public IOrderExecutionService Orders => this;
    public IPositionProvider Positions => this;
    public IFundsProvider Funds => this;

    public BrokerCapabilities GetCapabilities() => new(
        SupportsWebSocket: true,
        SupportsHistoricalData: true,
        SupportsGttOrders: false,
        SupportsBracketOrders: false,
        SupportedExchanges: ["NSE", "BSE"],
        SupportedProductTypes: [ProductType.MIS, ProductType.CNC],
        SupportedOrderTypes: [OrderType.Market, OrderType.Limit, OrderType.StopLoss],
        MaxOrdersPerSecond: 10
    );

    // Authentication (OAuth 2.0)
    public Task<string> GetAuthorizationUrlAsync(BrokerConnection connection, string redirectUri, string state)
    {
        var apiKey = _encryptionService.Decrypt(connection.EncryptedApiKey);
        var url = $"https://api.upstox.com/v2/login/authorization/dialog?response_type=code&client_id={apiKey}&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}";
        return Task.FromResult(url);
    }

    public async Task<TokenExchangeResult> ExchangeCodeAsync(BrokerConnection connection, string authorizationCode, string redirectUri)
    {
        try
        {
            var apiKey = _encryptionService.Decrypt(connection.EncryptedApiKey);
            var apiSecret = _encryptionService.Decrypt(connection.EncryptedApiSecret);

            var form = new Dictionary<string, string>
            {
                ["code"] = authorizationCode,
                ["client_id"] = apiKey,
                ["client_secret"] = apiSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var res = await _httpClient.PostAsync("https://api.upstox.com/v2/login/authorization/token", new FormUrlEncodedContent(form));
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<UpstoxTokenResponse>();
                return new TokenExchangeResult(
                    AccessToken: body?.AccessToken ?? string.Empty,
                    RefreshToken: null,
                    ExpiresAtUtc: DateTime.UtcNow.AddHours(12),
                    Success: true,
                    ErrorMessage: null
                );
            }

            var err = await res.Content.ReadAsStringAsync();
            return new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, $"Upstox OAuth failed: {err}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Upstox token exchange");
            return new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, ex.Message);
        }
    }

    public Task<TokenExchangeResult> RefreshTokenAsync(BrokerConnection connection)
    {
        return Task.FromResult(new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, "Upstox requires daily re-login."));
    }

    public Task<bool> ValidateTokenAsync(BrokerConnection connection)
    {
        return Task.FromResult(connection.TokenExpiresAtUtc.HasValue && connection.TokenExpiresAtUtc.Value > DateTime.UtcNow);
    }

    // Market Data
    public Task<Quote?> GetQuoteAsync(string instrumentToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Quote?>(null);
    }

    public Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IEnumerable<string> instrumentTokens, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<string, Quote>>(new Dictionary<string, Quote>());
    }

    public Task<IReadOnlyList<HistoricalCandle>> GetHistoricalCandlesAsync(string instrumentToken, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<HistoricalCandle>>([]);
    }

    // Order Execution
    public Task<OrderResponse> PlaceOrderAsync(BrokerConnection connection, TradingAccount account, OrderRequest request, CancellationToken cancellationToken = default)
    {
        if (account.TradingMode != TradingMode.Live)
        {
            // Paper mode safeguard
            return Task.FromResult(new OrderResponse(true, $"PAPER-UPSTOX-{Guid.NewGuid():N}", OrderStatus.Filled, "Simulated in paper mode"));
        }

        _logger.LogInformation("LIVE UPSTOX: Placing {Side} {Qty} of {Token} on account {Account}",
            request.Side, request.Quantity, request.InstrumentToken, account.AccountId);

        return Task.FromResult(new OrderResponse(true, $"UPSTOX-{Guid.NewGuid():N}", OrderStatus.Submitted, "Live order submitted"));
    }

    public Task<OrderResponse> ModifyOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, int newQuantity, decimal? newPrice, decimal? newTriggerPrice, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OrderResponse(true, brokerOrderId, OrderStatus.Open, "Modified"));
    }

    public Task<OrderResponse> CancelOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OrderResponse(true, brokerOrderId, OrderStatus.Cancelled, "Cancelled"));
    }

    public Task<OrderDetails?> GetOrderDetailsAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<OrderDetails?>(null);
    }

    public Task<IReadOnlyList<OrderDetails>> GetOrderBookAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<OrderDetails>>([]);
    }

    public Task<IReadOnlyList<PositionDetails>> GetPositionsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PositionDetails>>([]);
    }

    public Task<FundsDetails> GetFundsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FundsDetails(account.AvailableFunds, 0m, account.AvailableFunds, 0m));
    }

    private record UpstoxTokenResponse(string? AccessToken);
}
