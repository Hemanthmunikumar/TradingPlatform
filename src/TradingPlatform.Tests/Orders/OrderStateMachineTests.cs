using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Orders;
using Xunit;

namespace TradingPlatform.Tests.Orders;

public class OrderStateMachineTests
{
    private readonly OrderStateMachine _stateMachine = new();

    [Theory]
    [InlineData(OrderStatus.Created, OrderStatus.Submitted, true)]
    [InlineData(OrderStatus.Created, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Created, OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Created, OrderStatus.Filled, false)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Accepted, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Open, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Filled, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Submitted, OrderStatus.Error, true)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Open, true)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Filled, true)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Rejected, true)]
    [InlineData(OrderStatus.Open, OrderStatus.PartiallyFilled, true)]
    [InlineData(OrderStatus.Open, OrderStatus.Filled, true)]
    [InlineData(OrderStatus.Open, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Filled, OrderStatus.Open, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Submitted, false)]
    public void CanTransition_ValidatesAllowedTransitions(OrderStatus current, OrderStatus target, bool expected)
    {
        var result = _stateMachine.CanTransition(current, target);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Transition_WhenValid_UpdatesStatusAndEmitsOrderEvent()
    {
        var order = new TradingOrder
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Created
        };

        var orderEvent = _stateMachine.Transition(order, OrderStatus.Submitted, "Submitting to exchange");

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(OrderStatus.Created, orderEvent.PreviousStatus);
        Assert.Equal(OrderStatus.Submitted, orderEvent.NewStatus);
        Assert.Equal("OrderSubmitted", orderEvent.EventType);
        Assert.Contains(orderEvent, order.Events);
    }

    [Fact]
    public void Transition_WhenInvalid_ThrowsInvalidOperationException()
    {
        var order = new TradingOrder
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Filled
        };

        Assert.Throws<InvalidOperationException>(() =>
            _stateMachine.Transition(order, OrderStatus.Open, "Illegal reopen"));
    }
}
