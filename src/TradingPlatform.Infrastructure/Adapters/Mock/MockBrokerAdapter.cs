using System.Collections.Concurrent;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Adapters.Mock;

public class MockBrokerAdapter : IBrokerAdapter, IBrokerAuthentication, IMarketDataProvider, IOrderExecutionService, IPositionProvider, IFundsProvider
{
    public BrokerCode BrokerCode => BrokerCode.Mock;

    public IBrokerAuthentication Authentication => this;
    public IMarketDataProvider MarketData => this;
    public IOrderExecutionService Orders => this;
    public IPositionProvider Positions => this;
    public IFundsProvider Funds => this;

    // Simulated In-Memory State
    private readonly ConcurrentDictionary<string, OrderDetails> _orders = new();
    private readonly ConcurrentDictionary<string, PositionDetails> _positions = new();
    private decimal _availableCash = 500_000.00m; // ₹5 Lakh default virtual starting capital

    public BrokerCapabilities GetCapabilities() => new(
        SupportsWebSocket: true,
        SupportsHistoricalData: true,
        SupportsGttOrders: true,
        SupportsBracketOrders: false,
        SupportedExchanges: ["NSE", "BSE"],
        SupportedProductTypes: [ProductType.CNC, ProductType.MIS, ProductType.NRML],
        SupportedOrderTypes: [OrderType.Market, OrderType.Limit, OrderType.StopLoss, OrderType.StopLossMarket],
        MaxOrdersPerSecond: 10
    );

    #region Authentication
    public Task<string> GetAuthorizationUrlAsync(BrokerConnection connection, string redirectUri, string state)
    {
        var authUrl = $"http://localhost:5000/mock-broker/auth?client_id=mock_client&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}";
        return Task.FromResult(authUrl);
    }

    public Task<TokenExchangeResult> ExchangeCodeAsync(BrokerConnection connection, string authorizationCode, string redirectUri)
    {
        var result = new TokenExchangeResult(
            AccessToken: $"mock_access_token_{Guid.NewGuid():N}",
            RefreshToken: $"mock_refresh_token_{Guid.NewGuid():N}",
            ExpiresAtUtc: DateTime.UtcNow.AddHours(24),
            Success: true
        );
        return Task.FromResult(result);
    }

    public Task<TokenExchangeResult> RefreshTokenAsync(BrokerConnection connection)
    {
        var result = new TokenExchangeResult(
            AccessToken: $"mock_refreshed_token_{Guid.NewGuid():N}",
            RefreshToken: $"mock_refresh_token_{Guid.NewGuid():N}",
            ExpiresAtUtc: DateTime.UtcNow.AddHours(24),
            Success: true
        );
        return Task.FromResult(result);
    }

    public Task<bool> ValidateTokenAsync(BrokerConnection connection) => Task.FromResult(true);
    #endregion

    #region Market Data
    private static readonly Dictionary<string, (string Symbol, decimal Price)> DefaultStocks = new()
    {
        ["NSE_INFY"] = ("INFY", 1850.50m),
        ["NSE_TCS"] = ("TCS", 4125.00m),
        ["NSE_RELIANCE"] = ("RELIANCE", 2980.75m),
        ["NSE_HDFCBANK"] = ("HDFCBANK", 1720.30m),
        ["NSE_TATAMOTORS"] = ("TATAMOTORS", 965.40m),
        ["NSE_NIFTY50"] = ("NIFTY 50", 24350.00m)
    };

    public Task<Quote?> GetQuoteAsync(string instrumentToken, CancellationToken cancellationToken = default)
    {
        var baseStock = DefaultStocks.TryGetValue(instrumentToken, out var stock)
            ? stock
            : (Symbol: instrumentToken, Price: 1000.00m);

        var quote = new Quote(
            InstrumentToken: instrumentToken,
            TradingSymbol: baseStock.Symbol,
            Exchange: "NSE",
            LastPrice: baseStock.Price,
            Open: baseStock.Price * 0.995m,
            High: baseStock.Price * 1.015m,
            Low: baseStock.Price * 0.990m,
            Close: baseStock.Price,
            Change: baseStock.Price * 0.005m,
            ChangePercent: 0.50m,
            Volume: 1_250_000,
            BidPrice: baseStock.Price - 0.05m,
            AskPrice: baseStock.Price + 0.05m,
            TimestampUtc: DateTime.UtcNow
        );

        return Task.FromResult<Quote?>(quote);
    }

    public async Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IEnumerable<string> instrumentTokens, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, Quote>();
        foreach (var token in instrumentTokens)
        {
            var quote = await GetQuoteAsync(token, cancellationToken);
            if (quote != null)
            {
                dict[token] = quote;
            }
        }
        return dict;
    }

    public Task<IReadOnlyList<HistoricalCandle>> GetHistoricalCandlesAsync(string instrumentToken, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var basePrice = DefaultStocks.TryGetValue(instrumentToken, out var stock) ? stock.Price : 1000m;
        var candles = new List<HistoricalCandle>();
        var current = fromUtc;

        while (current <= toUtc)
        {
            candles.Add(new HistoricalCandle(
                TimestampUtc: current,
                Open: basePrice - 2m,
                High: basePrice + 5m,
                Low: basePrice - 4m,
                Close: basePrice + 1m,
                Volume: 50_000
            ));
            current = current.AddMinutes(5);
        }

        return Task.FromResult<IReadOnlyList<HistoricalCandle>>(candles);
    }
    #endregion

    #region Order Execution
    public async Task<OrderResponse> PlaceOrderAsync(BrokerConnection connection, TradingAccount account, OrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return new OrderResponse(false, null, OrderStatus.Rejected, "Order quantity must be positive.", "INVALID_QTY");
        }

        var brokerOrderId = $"MOCK-ORD-{DateTime.UtcNow.Ticks % 10000000:D7}";
        var quote = await GetQuoteAsync(request.InstrumentToken, cancellationToken);
        var execPrice = request.OrderType == OrderType.Market
            ? (quote?.LastPrice ?? request.Price)
            : request.Price;

        var orderValue = request.Quantity * execPrice;
        if (request.Side == OrderSide.Buy && orderValue > _availableCash)
        {
            return new OrderResponse(false, null, OrderStatus.Rejected, "Insufficient funds in simulated account.", "INSUFFICIENT_FUNDS");
        }

        // Simulate immediate fill in mock environment
        var orderDetails = new OrderDetails(
            BrokerOrderId: brokerOrderId,
            TradingSymbol: request.TradingSymbol,
            Side: request.Side,
            OrderType: request.OrderType,
            ProductType: request.ProductType,
            Quantity: request.Quantity,
            FilledQuantity: request.Quantity,
            Price: request.Price,
            AveragePrice: execPrice,
            Status: OrderStatus.Filled,
            StatusMessage: "Simulated order filled successfully.",
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: DateTime.UtcNow
        );

        _orders[brokerOrderId] = orderDetails;

        // Update virtual funds and positions
        if (request.Side == OrderSide.Buy)
        {
            _availableCash -= orderValue;
            _positions.AddOrUpdate(
                request.TradingSymbol,
                new PositionDetails(request.InstrumentToken, request.TradingSymbol, request.ProductType, request.Quantity, execPrice, execPrice, 0m, 0m),
                (_, existing) =>
                {
                    int totalQty = existing.Quantity + request.Quantity;
                    decimal totalCost = (existing.Quantity * existing.AverageBuyPrice) + orderValue;
                    decimal newAvg = totalQty > 0 ? totalCost / totalQty : 0m;
                    return existing with { Quantity = totalQty, AverageBuyPrice = newAvg, CurrentPrice = execPrice };
                });
        }
        else // Sell
        {
            _availableCash += orderValue;
            _positions.AddOrUpdate(
                request.TradingSymbol,
                new PositionDetails(request.InstrumentToken, request.TradingSymbol, request.ProductType, -request.Quantity, execPrice, execPrice, 0m, 0m),
                (_, existing) =>
                {
                    int remainingQty = Math.Max(0, existing.Quantity - request.Quantity);
                    decimal realized = (execPrice - existing.AverageBuyPrice) * Math.Min(existing.Quantity, request.Quantity);
                    return existing with { Quantity = remainingQty, RealizedPnL = existing.RealizedPnL + realized, CurrentPrice = execPrice };
                });
        }

        return new OrderResponse(true, brokerOrderId, OrderStatus.Filled, "Order placed and filled in mock broker.");
    }

    public Task<OrderResponse> ModifyOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, int newQuantity, decimal? newPrice, decimal? newTriggerPrice, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(brokerOrderId, out var existing))
        {
            return Task.FromResult(new OrderResponse(false, brokerOrderId, OrderStatus.Error, "Order not found.", "NOT_FOUND"));
        }

        if (existing.Status == OrderStatus.Filled || existing.Status == OrderStatus.Cancelled)
        {
            return Task.FromResult(new OrderResponse(false, brokerOrderId, existing.Status, "Cannot modify a filled or cancelled order.", "INVALID_STATE"));
        }

        var updated = existing with
        {
            Quantity = newQuantity,
            Price = newPrice ?? existing.Price,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _orders[brokerOrderId] = updated;
        return Task.FromResult(new OrderResponse(true, brokerOrderId, updated.Status, "Order modified successfully."));
    }

    public Task<OrderResponse> CancelOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default)
    {
        if (!_orders.TryGetValue(brokerOrderId, out var existing))
        {
            return Task.FromResult(new OrderResponse(false, brokerOrderId, OrderStatus.Error, "Order not found.", "NOT_FOUND"));
        }

        if (existing.Status == OrderStatus.Filled)
        {
            return Task.FromResult(new OrderResponse(false, brokerOrderId, OrderStatus.Filled, "Cannot cancel an already filled order.", "ALREADY_FILLED"));
        }

        var updated = existing with
        {
            Status = OrderStatus.Cancelled,
            StatusMessage = "Order cancelled by user.",
            UpdatedAtUtc = DateTime.UtcNow
        };

        _orders[brokerOrderId] = updated;
        return Task.FromResult(new OrderResponse(true, brokerOrderId, OrderStatus.Cancelled, "Order cancelled in mock broker."));
    }

    public Task<OrderDetails?> GetOrderDetailsAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(brokerOrderId, out var order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<OrderDetails>> GetOrderBookAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        var list = _orders.Values.OrderByDescending(o => o.CreatedAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<OrderDetails>>(list);
    }
    #endregion

    #region Positions and Funds
    public Task<IReadOnlyList<PositionDetails>> GetPositionsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        var list = _positions.Values.Where(p => p.Quantity != 0).ToList();
        return Task.FromResult<IReadOnlyList<PositionDetails>>(list);
    }

    public Task<FundsDetails> GetFundsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default)
    {
        decimal usedMargin = _positions.Values.Where(p => p.Quantity > 0).Sum(p => p.Quantity * p.AverageBuyPrice);
        var funds = new FundsDetails(
            AvailableCash: _availableCash,
            UsedMargin: usedMargin,
            TotalCapital: _availableCash + usedMargin,
            CollateralMargin: 0m
        );
        return Task.FromResult(funds);
    }
    #endregion
}
