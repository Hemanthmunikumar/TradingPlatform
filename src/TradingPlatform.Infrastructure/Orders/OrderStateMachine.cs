using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Orders;

public class OrderStateMachine : IOrderStateMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions = new()
    {
        [OrderStatus.Created] = [OrderStatus.Submitted, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Submitted] = [OrderStatus.Accepted, OrderStatus.Open, OrderStatus.Filled, OrderStatus.Rejected, OrderStatus.Error],
        [OrderStatus.Accepted] = [OrderStatus.Open, OrderStatus.Filled, OrderStatus.PartiallyFilled, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Open] = [OrderStatus.PartiallyFilled, OrderStatus.Filled, OrderStatus.Cancelled],
        [OrderStatus.PartiallyFilled] = [OrderStatus.PartiallyFilled, OrderStatus.Filled, OrderStatus.Cancelled],
        [OrderStatus.Error] = [OrderStatus.Open, OrderStatus.Rejected, OrderStatus.Cancelled],
        [OrderStatus.Filled] = [],
        [OrderStatus.Rejected] = [],
        [OrderStatus.Cancelled] = []
    };

    public bool CanTransition(OrderStatus currentStatus, OrderStatus targetStatus)
    {
        if (currentStatus == targetStatus) return true;
        return AllowedTransitions.TryGetValue(currentStatus, out var nextStates) && nextStates.Contains(targetStatus);
    }

    public void ValidateTransition(OrderStatus currentStatus, OrderStatus targetStatus)
    {
        if (!CanTransition(currentStatus, targetStatus))
        {
            throw new InvalidOperationException(
                $"Invalid order state transition from '{currentStatus}' to '{targetStatus}'.");
        }
    }

    public OrderEvent Transition(TradingOrder order, OrderStatus targetStatus, string? message = null, string? brokerResponse = null)
    {
        ValidateTransition(order.Status, targetStatus);

        var prevStatus = order.Status;
        order.Status = targetStatus;
        if (!string.IsNullOrWhiteSpace(message))
        {
            order.StatusMessage = message;
        }

        var orderEvent = new OrderEvent
        {
            Id = Guid.NewGuid(),
            TradingOrderId = order.Id,
            TradingOrder = order,
            PreviousStatus = prevStatus,
            NewStatus = targetStatus,
            EventType = $"Order{targetStatus}",
            Message = message,
            BrokerResponseJson = brokerResponse
        };

        order.Events.Add(orderEvent);
        return orderEvent;
    }
}
