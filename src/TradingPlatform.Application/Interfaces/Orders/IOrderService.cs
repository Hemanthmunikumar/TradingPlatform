using TradingPlatform.Contracts.Orders;

namespace TradingPlatform.Application.Interfaces.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDto> SubmitOrderAsync(Guid orderId, CancellationToken ct = default);
    Task<OrderDto> ProcessOrderExecutionAsync(Guid orderId, int executedQuantity, decimal executionPrice, string? brokerTradeId = null, CancellationToken ct = default);
    Task<bool> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(OrderFilterRequest filter, CancellationToken ct = default);
}
