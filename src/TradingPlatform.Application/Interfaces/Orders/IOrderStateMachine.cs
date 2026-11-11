using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Application.Interfaces.Orders;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus currentStatus, OrderStatus targetStatus);
    void ValidateTransition(OrderStatus currentStatus, OrderStatus targetStatus);
    OrderEvent Transition(TradingOrder order, OrderStatus targetStatus, string? message = null, string? brokerResponse = null);
}
