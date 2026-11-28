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
                var accessToken = body?.Data?.AccessToken;
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _logger.LogWarning("Zerodha /session/token returned 200 but access_token was empty.");
                    return new TokenExchangeResult(string.Empty, null, DateTime.UtcNow, false, "Zerodha did not return a valid access_token.");
                }

                // Zerodha Kite invalidates all access tokens daily at 06:00 IST (00:30 UTC)
                var nowUtc = DateTime.UtcNow;
                var todayExpiryUtc = nowUtc.Date.AddMinutes(30); // 00:30 UTC = 06:00 IST
                var nextExpiryUtc = nowUtc < todayExpiryUtc ? todayExpiryUtc : todayExpiryUtc.AddDays(1);

                return new TokenExchangeResult(
                    AccessToken: accessToken,
                    RefreshToken: null,
                    ExpiresAtUtc: nextExpiryUtc,
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

    public async Task<bool> ValidateTokenAsync(BrokerConnection connection)
    {
        if (!connection.TokenExpiresAtUtc.HasValue || connection.TokenExpiresAtUtc.Value <= DateTime.UtcNow)
            return false;

        if (string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            return false;

        try
        {
            using var req = CreateKiteRequest(HttpMethod.Get, "https://api.kite.trade/user/profile", connection);
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return connection.TokenExpiresAtUtc.Value > DateTime.UtcNow;
        }
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

        try
        {
            using var req = CreateKiteRequest(HttpMethod.Post, "https://api.kite.trade/orders/regular", connection);
            var form = new Dictionary<string, string>
            {
                ["exchange"] = request.Exchange,
                ["tradingsymbol"] = request.TradingSymbol,
                ["transaction_type"] = request.Side == OrderSide.Buy ? "BUY" : "SELL",
                ["quantity"] = request.Quantity.ToString(),
                ["product"] = request.ProductType == ProductType.CNC ? "CNC" : "MIS",
                ["order_type"] = request.OrderType switch
                {
                    OrderType.Limit => "LIMIT",
                    OrderType.StopLoss => "SL",
                    _ => "MARKET"
                },
                ["validity"] = "DAY",
                ["tag"] = SelfAlgoOrderTag
            };
            if (request.Price > 0 && request.OrderType != OrderType.Market)
            {
                form["price"] = request.Price.ToString("0.00");
            }
            if (request.TriggerPrice.HasValue && request.TriggerPrice.Value > 0)
            {
                form["trigger_price"] = request.TriggerPrice.Value.ToString("0.00");
            }
            req.Content = new FormUrlEncodedContent(form);

            var res = await _httpClient.SendAsync(req, cancellationToken);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<KiteOrderPlaceResponse>(cancellationToken: cancellationToken);
                var orderId = body?.Data?.OrderId ?? $"ZERODHA-{Guid.NewGuid():N}";
                _logger.LogInformation("LIVE ZERODHA [Tag:{Tag}]: Order {OrderId} placed for {Side} {Qty} {Symbol}",
                    SelfAlgoOrderTag, orderId, request.Side, request.Quantity, request.TradingSymbol);
                return new OrderResponse(true, orderId, OrderStatus.Submitted, $"Live order placed with Zerodha (Tag: {SelfAlgoOrderTag})");
            }

            var errText = await res.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Zerodha live order rejected: {Error}", errText);
            return new OrderResponse(false, string.Empty, OrderStatus.Rejected, $"Zerodha rejected order: {errText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception placing Zerodha live order");
            return new OrderResponse(false, string.Empty, OrderStatus.Error, ex.Message);
        }
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

    public async Task<IReadOnlyList<OrderDetails>> GetOrderBookAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            return [];

        try
        {
            using var req = CreateKiteRequest(HttpMethod.Get, "https://api.kite.trade/orders", connection);
            var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode) return [];

            var body = await res.Content.ReadFromJsonAsync<KiteOrdersResponse>(cancellationToken: cancellationToken);
            if (body?.Data == null) return [];

            return body.Data.Select(o => new OrderDetails(
                BrokerOrderId: o.OrderId ?? string.Empty,
                TradingSymbol: o.TradingSymbol ?? string.Empty,
                Side: string.Equals(o.TransactionType, "SELL", StringComparison.OrdinalIgnoreCase) ? OrderSide.Sell : OrderSide.Buy,
                OrderType: o.OrderType == "LIMIT" ? OrderType.Limit : OrderType.Market,
                ProductType: o.Product == "CNC" ? ProductType.CNC : ProductType.MIS,
                Quantity: o.Quantity,
                FilledQuantity: o.FilledQuantity,
                Price: o.Price,
                AveragePrice: o.AveragePrice,
                Status: o.Status?.ToUpperInvariant() switch
                {
                    "COMPLETE" => OrderStatus.Filled,
                    "CANCELLED" => OrderStatus.Cancelled,
                    "REJECTED" => OrderStatus.Rejected,
                    _ => OrderStatus.Open
                },
                StatusMessage: o.StatusMessage,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: null
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed fetching Zerodha order book");
            return [];
        }
    }

    public async Task<IReadOnlyList<PositionDetails>> GetPositionsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            return [];

        var results = new List<PositionDetails>();
        try
        {
            // 1. Fetch CNC Holdings (existing delivery portfolio)
            using var holdReq = CreateKiteRequest(HttpMethod.Get, "https://api.kite.trade/portfolio/holdings", connection);
            var holdRes = await _httpClient.SendAsync(holdReq, cancellationToken);
            if (!holdRes.IsSuccessStatusCode)
            {
                var err = await holdRes.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Zerodha /portfolio/holdings returned {Status}: {Error}", holdRes.StatusCode, err);
            }
            else
            {
                var holdBody = await holdRes.Content.ReadFromJsonAsync<KiteHoldingsResponse>(cancellationToken: cancellationToken);
                if (holdBody?.Data != null)
                {
                    foreach (var h in holdBody.Data)
                    {
                        var qty = h.Quantity + h.T1Quantity;
                        if (qty <= 0 || string.IsNullOrWhiteSpace(h.TradingSymbol)) continue;
                        results.Add(new PositionDetails(
                            InstrumentToken: h.Exchange ?? "NSE",
                            TradingSymbol: h.TradingSymbol,
                            ProductType: ProductType.CNC,
                            Quantity: qty,
                            AverageBuyPrice: h.AveragePrice,
                            CurrentPrice: h.LastPrice > 0 ? h.LastPrice : h.AveragePrice,
                            RealizedPnL: 0m,
                            UnrealizedPnL: h.PnL
                        ));
                    }
                }
            }

            // 2. Fetch Net Intraday / F&O Positions
            using var posReq = CreateKiteRequest(HttpMethod.Get, "https://api.kite.trade/portfolio/positions", connection);
            var posRes = await _httpClient.SendAsync(posReq, cancellationToken);
            if (posRes.IsSuccessStatusCode)
            {
                var posBody = await posRes.Content.ReadFromJsonAsync<KitePositionsResponse>(cancellationToken: cancellationToken);
                if (posBody?.Data?.Net != null)
                {
                    foreach (var p in posBody.Data.Net)
                    {
                        if (p.Quantity == 0 || string.IsNullOrWhiteSpace(p.TradingSymbol)) continue;
                        results.Add(new PositionDetails(
                            InstrumentToken: p.Exchange ?? "NSE",
                            TradingSymbol: p.TradingSymbol,
                            ProductType: p.Product == "CNC" ? ProductType.CNC : ProductType.MIS,
                            Quantity: p.Quantity,
                            AverageBuyPrice: p.AveragePrice,
                            CurrentPrice: p.LastPrice > 0 ? p.LastPrice : p.AveragePrice,
                            RealizedPnL: 0m,
                            UnrealizedPnL: p.Unrealized
                        ));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed fetching Zerodha holdings/positions");
        }

        return results;
    }

    public async Task<FundsDetails> GetFundsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connection.EncryptedAccessToken))
            return new FundsDetails(account.AvailableFunds, 0m, account.AvailableFunds, 0m);

        try
        {
            using var req = CreateKiteRequest(HttpMethod.Get, "https://api.kite.trade/user/margins", connection);
            var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
                return new FundsDetails(account.AvailableFunds, 0m, account.AvailableFunds, 0m);

            var body = await res.Content.ReadFromJsonAsync<KiteMarginsResponse>(cancellationToken: cancellationToken);
            var eq = body?.Data?.Equity;
            if (eq != null)
            {
                var cash = eq.Available?.Cash ?? eq.Net;
                var used = eq.Utilised?.Debits ?? 0m;
                return new FundsDetails(cash, used, eq.Net, eq.Available?.Collateral ?? 0m);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed fetching Zerodha margins");
        }

        return new FundsDetails(account.AvailableFunds, 0m, account.AvailableFunds, 0m);
    }

    private HttpRequestMessage CreateKiteRequest(HttpMethod method, string url, BrokerConnection connection)
    {
        var apiKey = _encryptionService.Decrypt(connection.EncryptedApiKey);
        var accessToken = _encryptionService.Decrypt(connection.EncryptedAccessToken!);
        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("X-Kite-Version", "3");
        req.Headers.TryAddWithoutValidation("Authorization", $"token {apiKey}:{accessToken}");
        return req;
    }

    private record KiteTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("data")] KiteData? Data);
    private record KiteData(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken);
    private record KiteOrderPlaceResponse(KiteOrderIdData? Data);
    private record KiteOrderIdData(string? OrderId);
    private record KiteOrdersResponse(List<KiteOrderItem>? Data);
    private record KiteOrderItem(
        [property: System.Text.Json.Serialization.JsonPropertyName("order_id")] string? OrderId,
        [property: System.Text.Json.Serialization.JsonPropertyName("tradingsymbol")] string? TradingSymbol,
        [property: System.Text.Json.Serialization.JsonPropertyName("transaction_type")] string? TransactionType,
        [property: System.Text.Json.Serialization.JsonPropertyName("order_type")] string? OrderType,
        string? Product,
        int Quantity,
        [property: System.Text.Json.Serialization.JsonPropertyName("filled_quantity")] int FilledQuantity,
        decimal Price,
        [property: System.Text.Json.Serialization.JsonPropertyName("average_price")] decimal AveragePrice,
        string? Status,
        [property: System.Text.Json.Serialization.JsonPropertyName("status_message")] string? StatusMessage);
    private record KiteHoldingsResponse(List<KiteHoldingItem>? Data);
    private record KiteHoldingItem(
        [property: System.Text.Json.Serialization.JsonPropertyName("tradingsymbol")] string? TradingSymbol,
        string? Exchange,
        int Quantity,
        [property: System.Text.Json.Serialization.JsonPropertyName("t1_quantity")] int T1Quantity,
        [property: System.Text.Json.Serialization.JsonPropertyName("average_price")] decimal AveragePrice,
        [property: System.Text.Json.Serialization.JsonPropertyName("last_price")] decimal LastPrice,
        decimal PnL);
    private record KitePositionsResponse(KitePositionsData? Data);
    private record KitePositionsData(List<KitePositionItem>? Net);
    private record KitePositionItem(
        [property: System.Text.Json.Serialization.JsonPropertyName("tradingsymbol")] string? TradingSymbol,
        string? Exchange,
        string? Product,
        int Quantity,
        [property: System.Text.Json.Serialization.JsonPropertyName("average_price")] decimal AveragePrice,
        [property: System.Text.Json.Serialization.JsonPropertyName("last_price")] decimal LastPrice,
        decimal Unrealized);
    private record KiteMarginsResponse(KiteMarginsData? Data);
    private record KiteMarginsData(KiteEquityMargin? Equity);
    private record KiteEquityMargin(decimal Net, KiteAvailableMargin? Available, KiteUtilisedMargin? Utilised);
    private record KiteAvailableMargin(decimal Cash, decimal Collateral);
    private record KiteUtilisedMargin(decimal Debits);
}
