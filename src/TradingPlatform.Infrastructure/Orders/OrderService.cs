using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;

namespace TradingPlatform.Infrastructure.Orders;

public class OrderService(
    TradingDbContext dbContext,
    IOrderStateMachine stateMachine,
    IPositionService positionService,
    IBrokerAdapterFactory brokerAdapterFactory,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var account = await dbContext.TradingAccounts
            .FirstOrDefaultAsync(a => a.Id == request.TradingAccountId, ct);

        if (account == null)
        {
            throw new ArgumentException($"Trading account {request.TradingAccountId} not found.");
        }

        var instrument = await dbContext.Instruments
            .FirstOrDefaultAsync(i => i.Id == request.InstrumentId, ct);

        if (instrument == null)
        {
            throw new ArgumentException($"Instrument {request.InstrumentId} not found.");
        }

        if (!Enum.TryParse<OrderSide>(request.Side, true, out var side))
        {
            side = OrderSide.Buy;
        }

        if (!Enum.TryParse<OrderType>(request.OrderType, true, out var orderType))
        {
            orderType = OrderType.Market;
        }

        if (!Enum.TryParse<ProductType>(request.ProductType, true, out var productType))
        {
            productType = ProductType.MIS;
        }

        var order = new TradingOrder
        {
            Id = Guid.NewGuid(),
            UserId = account.UserId,
            TradingAccountId = request.TradingAccountId,
            TradingMode = account.TradingMode,
            InstrumentId = request.InstrumentId,
            StrategyVersionId = request.StrategyVersionId,
            PendingTradeApprovalId = request.PendingTradeApprovalId,
            Side = side,
            OrderType = orderType,
            ProductType = productType,
            Quantity = request.Quantity,
            FilledQuantity = 0,
            Price = request.Price,
            TriggerPrice = request.TriggerPrice ?? request.StopLoss,
            AveragePrice = 0m,
            Status = OrderStatus.Created,
            StatusMessage = "Order created",
            IdempotencyKey = Guid.NewGuid()
        };

        // Create initial creation event
        RecordTransition(order, OrderStatus.Created, "Order created");

        dbContext.TradingOrders.Add(order);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Created TradingOrder {OrderId} for {Side} {Quantity} {Symbol}", order.Id, order.Side, order.Quantity, instrument.TradingSymbol);

        return MapToDto(order, instrument);
    }

    public async Task<OrderDto> SubmitOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Include(o => o.TradingAccount)
                .ThenInclude(a => a.BrokerConnection)
                    .ThenInclude(c => c!.BrokerProvider)
            .Include(o => o.Events)
            .Include(o => o.Executions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            throw new ArgumentException($"TradingOrder {orderId} not found.");
        }

        RecordTransition(order, OrderStatus.Submitted, "Order submitted to broker");
        order.SubmittedAtUtc = DateTime.UtcNow;

        var account = order.TradingAccount;
        var connection = account.BrokerConnection;

        // Paper trading or Mock broker simulation
        if (account.TradingMode == TradingMode.Paper || connection?.BrokerProvider?.Code == BrokerCode.Mock || connection == null)
        {
            var simulatedBrokerOrderId = $"MOCK-ORD-{DateTime.UtcNow.Ticks % 10000000:D7}";
            order.BrokerOrderId = simulatedBrokerOrderId;
            RecordTransition(order, OrderStatus.Accepted, $"Broker accepted order {simulatedBrokerOrderId}");
            await dbContext.SaveChangesAsync(ct);

            // Execute immediate fill in paper trading
            return await ProcessOrderExecutionAsync(order.Id, order.Quantity, order.Price, $"TRD-{DateTime.UtcNow.Ticks % 10000000:D7}", ct);
        }

        // Live broker dispatch
        try
        {
            var adapter = brokerAdapterFactory.GetAdapter(connection.BrokerProvider.Code);
            var brokerRequest = new OrderRequest(
                InstrumentToken: order.Instrument.InstrumentToken ?? order.Instrument.TradingSymbol,
                TradingSymbol: order.Instrument.TradingSymbol,
                Exchange: order.Instrument.Exchange,
                Side: order.Side,
                OrderType: order.OrderType,
                ProductType: order.ProductType,
                Quantity: order.Quantity,
                Price: order.Price,
                TriggerPrice: order.TriggerPrice,
                IdempotencyKey: order.IdempotencyKey
            );

            var brokerResponse = await adapter.Orders.PlaceOrderAsync(connection, account, brokerRequest, ct);

            if (brokerResponse.Success && !string.IsNullOrWhiteSpace(brokerResponse.BrokerOrderId))
            {
                order.BrokerOrderId = brokerResponse.BrokerOrderId;
                RecordTransition(order, OrderStatus.Accepted, $"Broker accepted order: {brokerResponse.Message}", brokerResponse.ErrorCode);

                // If broker reported immediate fill
                if (brokerResponse.Status == OrderStatus.Filled)
                {
                    await dbContext.SaveChangesAsync(ct);
                    return await ProcessOrderExecutionAsync(order.Id, order.Quantity, order.Price, null, ct);
                }
            }
            else
            {
                RecordTransition(order, OrderStatus.Rejected, brokerResponse.Message ?? "Broker rejected order", brokerResponse.ErrorCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit order {OrderId} to broker", order.Id);
            RecordTransition(order, OrderStatus.Error, $"Submission failed: {ex.Message}");
        }

        await dbContext.SaveChangesAsync(ct);
        return MapToDto(order, order.Instrument);
    }

    public async Task<OrderDto> ProcessOrderExecutionAsync(
        Guid orderId,
        int executedQuantity,
        decimal executionPrice,
        string? brokerTradeId = null,
        CancellationToken ct = default)
    {
        var order = await dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Include(o => o.Events)
            .Include(o => o.Executions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            throw new ArgumentException($"TradingOrder {orderId} not found.");
        }

        var execution = new TradeExecution
        {
            Id = Guid.NewGuid(),
            UserId = order.UserId,
            TradingOrderId = order.Id,
            TradingOrder = order,
            TradingAccountId = order.TradingAccountId,
            TradingMode = order.TradingMode,
            InstrumentId = order.InstrumentId,
            Instrument = order.Instrument,
            BrokerTradeId = brokerTradeId ?? $"TRD-{Guid.NewGuid():N}"[..16],
            Side = order.Side,
            Quantity = executedQuantity,
            Price = executionPrice,
            Exchange = order.Instrument.Exchange,
            ExecutedAtUtc = DateTime.UtcNow
        };

        dbContext.TradeExecutions.Add(execution);
        order.Executions.Add(execution);

        var newFilled = order.FilledQuantity + executedQuantity;
        if (newFilled > 0)
        {
            order.AveragePrice = ((order.FilledQuantity * order.AveragePrice) + (executedQuantity * executionPrice)) / newFilled;
        }
        order.FilledQuantity = newFilled;

        if (order.FilledQuantity >= order.Quantity)
        {
            RecordTransition(order, OrderStatus.Filled, $"100% Filled ({order.FilledQuantity}/{order.Quantity}) at avg price {order.AveragePrice:F2}");
        }
        else
        {
            RecordTransition(order, OrderStatus.PartiallyFilled, $"Partially filled ({order.FilledQuantity}/{order.Quantity}) at avg price {order.AveragePrice:F2}");
        }

        // Update position & accounting
        await positionService.UpdatePositionOnExecutionAsync(execution, ct);

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Processed execution for Order {OrderId}: filled {ExecutedQty} @ {Price}, total filled {Filled}/{Total}",
            order.Id, executedQuantity, executionPrice, order.FilledQuantity, order.Quantity);

        return MapToDto(order, order.Instrument);
    }

    public async Task<bool> CancelOrderAsync(CancelOrderRequest request, CancellationToken ct = default)
    {
        var order = await dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Include(o => o.TradingAccount)
                .ThenInclude(a => a.BrokerConnection)
                    .ThenInclude(c => c!.BrokerProvider)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order == null) return false;

        if (order.Status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected)
        {
            return false;
        }

        // If live order on broker
        if (order.TradingAccount.TradingMode == TradingMode.Live &&
            !string.IsNullOrWhiteSpace(order.BrokerOrderId) &&
            order.TradingAccount.BrokerConnection?.BrokerProvider != null)
        {
            try
            {
                var adapter = brokerAdapterFactory.GetAdapter(order.TradingAccount.BrokerConnection.BrokerProvider.Code);
                await adapter.Orders.CancelOrderAsync(order.TradingAccount.BrokerConnection, order.TradingAccount, order.BrokerOrderId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Broker cancel request failed for order {OrderId}", order.Id);
            }
        }

        RecordTransition(order, OrderStatus.Cancelled, request.Reason ?? "Cancelled by user");
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    private OrderEvent RecordTransition(TradingOrder order, OrderStatus newStatus, string? message = null, string? brokerResponse = null)
    {
        var evt = stateMachine.Transition(order, newStatus, message, brokerResponse);
        dbContext.OrderEvents.Add(evt);
        return evt;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Include(o => o.Events)
            .Include(o => o.Executions)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order == null ? null : MapToDto(order, order.Instrument);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(OrderFilterRequest filter, CancellationToken ct = default)
    {
        var query = dbContext.TradingOrders
            .Include(o => o.Instrument)
            .Include(o => o.Events)
            .Include(o => o.Executions)
            .Where(o => o.TradingAccount.IsActive
                && o.TradingAccount.BrokerConnection.IsActive
                && o.TradingMode == o.TradingAccount.TradingMode)
            .AsQueryable();

        if (filter.TradingAccountId.HasValue)
        {
            query = query.Where(o => o.TradingAccountId == filter.TradingAccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Symbol))
        {
            query = query.Where(o => o.Instrument.TradingSymbol == filter.Symbol);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<OrderStatus>(filter.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Side) && Enum.TryParse<OrderSide>(filter.Side, true, out var side))
        {
            query = query.Where(o => o.Side == side);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(o => o.CreatedAtUtc <= filter.ToUtc.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return orders.Select(o => MapToDto(o, o.Instrument)).ToList();
    }

    private static OrderDto MapToDto(TradingOrder order, Instrument? instrument)
    {
        var events = order.Events
            .OrderBy(e => e.CreatedAtUtc)
            .Select(e => new OrderEventDto(
                Id: e.Id,
                OrderId: e.TradingOrderId,
                PreviousStatus: e.PreviousStatus.ToString(),
                NewStatus: e.NewStatus.ToString(),
                EventType: e.EventType,
                Message: e.Message,
                CreatedAtUtc: e.CreatedAtUtc
            ))
            .ToList();

        var executions = order.Executions
            .OrderBy(e => e.ExecutedAtUtc)
            .Select(e => new TradeExecutionDto(
                Id: e.Id,
                OrderId: e.TradingOrderId,
                TradingAccountId: e.TradingAccountId,
                InstrumentId: e.InstrumentId,
                Symbol: instrument?.TradingSymbol ?? string.Empty,
                Side: e.Side.ToString(),
                Quantity: e.Quantity,
                Price: e.Price,
                TotalValue: e.TotalValue,
                Exchange: e.Exchange,
                ExecutedAtUtc: e.ExecutedAtUtc
            ))
            .ToList();

        return new OrderDto(
            Id: order.Id,
            TradingAccountId: order.TradingAccountId,
            InstrumentId: order.InstrumentId,
            Symbol: instrument?.TradingSymbol ?? string.Empty,
            CompanyName: instrument?.CompanyName ?? string.Empty,
            Side: order.Side.ToString(),
            OrderType: order.OrderType.ToString(),
            ProductType: order.ProductType.ToString(),
            Quantity: order.Quantity,
            FilledQuantity: order.FilledQuantity,
            Price: order.Price,
            TriggerPrice: order.TriggerPrice,
            AveragePrice: order.AveragePrice,
            Status: order.Status.ToString(),
            StatusMessage: order.StatusMessage,
            BrokerOrderId: order.BrokerOrderId,
            IdempotencyKey: order.IdempotencyKey,
            CreatedAtUtc: order.CreatedAtUtc,
            SubmittedAtUtc: order.SubmittedAtUtc,
            Events: events,
            Executions: executions
        );
    }
}
