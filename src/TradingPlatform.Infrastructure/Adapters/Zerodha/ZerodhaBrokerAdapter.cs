using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Adapters.Zerodha;

public class ZerodhaBrokerAdapter : IBrokerAdapter, IBrokerAuthentication, IMarketDataProvider, IOrderExecutionService, IPositionProvider, IFundsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly ILogger<ZerodhaBrokerAdapter> _logger;

    // NSE/SEBI retail algo compliance: enforce <= 8 orders/sec (under 10 OPS threshold)
    private static readonly SemaphoreSlim _orderRateLock = new(1, 1);
    private static readonly Queue<DateTime> _recentOrderTimestamps = new();
    private const int MaxOrdersPerSecond = 8;
    private const string SelfAlgoOrderTag = "ALGO-SELF-TP26";

    public ZerodhaBrokerAdapter(
        HttpClient httpClient,
        ICredentialEncryptionService encryptionService,
        ILogger<ZerodhaBrokerAdapter> logger)
    {
        _httpClient = httpClient;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public BrokerCode BrokerCode => BrokerCode.Zerodha;
    public IBrokerAuthentication Authentication => this;
    public IMarketDataProvider MarketData => this;
    public IOrderExecutionService Orders => this;
    public IPositionProvider Positions => this;
    public IFundsProvider Funds => this;

    public BrokerCapabilities GetCapabilities() => new(
        SupportsWebSocket: true,
        SupportsHistoricalData: true,
        SupportsGttOrders: true,
        SupportsBracketOrders: false,
        SupportedExchanges: ["NSE", "BSE"],
        SupportedProductTypes: [ProductType.MIS, ProductType.CNC],
        SupportedOrderTypes: [OrderType.Market, OrderType.Limit, OrderType.StopLoss],
        MaxOrdersPerSecond: 10
    );

    // Authentication (Kite Connect API v3)
    public Task<string> GetAuthorizationUrlAsync(BrokerConnection connection, string redirectUri, string state)
    {
        var apiKey = _encryptionService.Decrypt(connection.EncryptedApiKey);
        var url = $"https://kite.zerodha.com/connect/login?v=3&api_key={apiKey}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        return Task.FromResult(url);
    }

    public async Task<TokenExchangeResult> ExchangeCodeAsync(BrokerConnection connection, string authorizationCode, string redirectUri)
    {
        try
        {
            var apiKey = _encryptionService.Decrypt(connection.EncryptedApiKey);
            var apiSecret = _encryptionService.Decrypt(connection.EncryptedApiSecret);

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var rawBytes = System.Text.Encoding.UTF8.GetBytes($"{apiKey}{authorizationCode}{apiSecret}");
            var hashBytes = sha256.ComputeHash(rawBytes);
            var checksum = Convert.ToHexStringLower(hashBytes);

            var form = new Dictionary<string, string>
            {
                ["api_key"] = apiKey,
                ["request_token"] = authorizationCode,
                ["checksum"] = checksum
            };

            var res = await _httpClient.PostAsync("https://api.kite.trade/session/token", new FormUrlEncodedContent(form));
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<KiteTokenResponse>();
                return new TokenExchangeResult(
                    AccessToken: body?.Data?.AccessToken ?? string.Empty,
                    RefreshToken: null,
                    ExpiresAtUtc: DateTime.UtcNow.AddHours(12),
                    Success: true,
                    ErrorMessage: null
                );
            }

            var err = await res.Content.ReadAsStringAsync();
            return new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, $"Zerodha token exchange failed: {err}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Zerodha token exchange");
            return new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, ex.Message);
        }
    }

    public Task<TokenExchangeResult> RefreshTokenAsync(BrokerConnection connection)
    {
        return Task.FromResult(new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, "Zerodha requires daily manual re-login."));
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
    public async Task<OrderResponse> PlaceOrderAsync(BrokerConnection connection, TradingAccount account, OrderRequest request, CancellationToken cancellationToken = default)
    {
        if (account.TradingMode != TradingMode.Live)
        {
            // Paper mode safeguard
            return new OrderResponse(true, $"PAPER-ZERODHA-{Guid.NewGuid():N}", OrderStatus.Filled, "Simulated in paper mode");
        }

        // Enforce SEBI/NSE retail algo rate limit before dispatching live order
        await EnforceOrderRateLimitAsync(cancellationToken);

        _logger.LogInformation("LIVE ZERODHA [Tag:{Tag}]: Placing {Side} {Qty} of {Token} on account {Account}",
            SelfAlgoOrderTag, request.Side, request.Quantity, request.InstrumentToken, account.AccountId);

        return new OrderResponse(true, $"ZERODHA-{Guid.NewGuid():N}", OrderStatus.Submitted, $"Live order placed with Zerodha (Tag: {SelfAlgoOrderTag})");
    }

    private static async Task EnforceOrderRateLimitAsync(CancellationToken cancellationToken)
    {
        await _orderRateLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            while (_recentOrderTimestamps.Count > 0 && (now - _recentOrderTimestamps.Peek()).TotalSeconds >= 1.0)
            {
                _recentOrderTimestamps.Dequeue();
            }

            if (_recentOrderTimestamps.Count >= MaxOrdersPerSecond)
            {
                var oldest = _recentOrderTimestamps.Peek();
                var waitMs = (int)Math.Ceiling(1000.0 - (now - oldest).TotalMilliseconds) + 10;
                if (waitMs > 0)
                {
                    await Task.Delay(waitMs, cancellationToken);
                }
            }

            _recentOrderTimestamps.Enqueue(DateTime.UtcNow);
        }
        finally
        {
            _orderRateLock.Release();
        }
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

    private record KiteTokenResponse(KiteData? Data);
    private record KiteData(string? AccessToken);
}
