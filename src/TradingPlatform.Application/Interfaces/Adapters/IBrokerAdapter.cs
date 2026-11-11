using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Application.Interfaces.Adapters;

public interface IBrokerAdapter
{
    BrokerCode BrokerCode { get; }
    IBrokerAuthentication Authentication { get; }
    IMarketDataProvider MarketData { get; }
    IOrderExecutionService Orders { get; }
    IPositionProvider Positions { get; }
    IFundsProvider Funds { get; }
    BrokerCapabilities GetCapabilities();
}

public interface IBrokerAuthentication
{
    Task<string> GetAuthorizationUrlAsync(BrokerConnection connection, string redirectUri, string state);
    Task<TokenExchangeResult> ExchangeCodeAsync(BrokerConnection connection, string authorizationCode, string redirectUri);
    Task<TokenExchangeResult> RefreshTokenAsync(BrokerConnection connection);
    Task<bool> ValidateTokenAsync(BrokerConnection connection);
}

public interface IMarketDataProvider
{
    Task<Quote?> GetQuoteAsync(string instrumentToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Quote>> GetQuotesAsync(IEnumerable<string> instrumentTokens, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistoricalCandle>> GetHistoricalCandlesAsync(string instrumentToken, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}

public interface IOrderExecutionService
{
    Task<OrderResponse> PlaceOrderAsync(BrokerConnection connection, TradingAccount account, OrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderResponse> ModifyOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, int newQuantity, decimal? newPrice, decimal? newTriggerPrice, CancellationToken cancellationToken = default);
    Task<OrderResponse> CancelOrderAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default);
    Task<OrderDetails?> GetOrderDetailsAsync(BrokerConnection connection, TradingAccount account, string brokerOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDetails>> GetOrderBookAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default);
}

public interface IPositionProvider
{
    Task<IReadOnlyList<PositionDetails>> GetPositionsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default);
}

public interface IFundsProvider
{
    Task<FundsDetails> GetFundsAsync(BrokerConnection connection, TradingAccount account, CancellationToken cancellationToken = default);
}

public interface IBrokerAdapterFactory
{
    IBrokerAdapter GetAdapter(BrokerCode brokerCode);
}
