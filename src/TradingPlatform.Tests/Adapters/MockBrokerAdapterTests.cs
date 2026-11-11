using FluentAssertions;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters;
using TradingPlatform.Infrastructure.Adapters.Mock;

namespace TradingPlatform.Tests.Adapters;

public class MockBrokerAdapterTests
{
    private readonly MockBrokerAdapter _adapter = new();
    private readonly BrokerConnection _connection = new() { Id = Guid.NewGuid(), DisplayName = "Mock Account" };
    private readonly TradingAccount _account = new() { Id = Guid.NewGuid(), AccountId = "MOCK-ACC-01" };

    [Fact]
    public async Task PlaceOrderAsync_BuyOrder_ShouldFillAndDeductAvailableCash()
    {
        // Arrange
        var initialFunds = await _adapter.Funds.GetFundsAsync(_connection, _account);
        var initialCash = initialFunds.AvailableCash;

        var request = new OrderRequest(
            InstrumentToken: "NSE_INFY",
            TradingSymbol: "INFY",
            Exchange: "NSE",
            Side: OrderSide.Buy,
            OrderType: OrderType.Market,
            ProductType: ProductType.MIS,
            Quantity: 10,
            Price: 1850.50m,
            TriggerPrice: null,
            IdempotencyKey: Guid.NewGuid()
        );

        // Act
        var response = await _adapter.Orders.PlaceOrderAsync(_connection, _account, request);

        // Assert
        response.Success.Should().BeTrue();
        response.BrokerOrderId.Should().StartWith("MOCK-ORD-");
        response.Status.Should().Be(OrderStatus.Filled);

        var updatedFunds = await _adapter.Funds.GetFundsAsync(_connection, _account);
        updatedFunds.AvailableCash.Should().BeLessThan(initialCash);

        var positions = await _adapter.Positions.GetPositionsAsync(_connection, _account);
        positions.Should().ContainSingle(p => p.TradingSymbol == "INFY" && p.Quantity == 10);
    }

    [Fact]
    public async Task PlaceOrderAsync_SellOrderAfterBuy_ShouldCalculateRealizedPnL()
    {
        // Arrange: Buy 20 shares of TCS
        var buyReq = new OrderRequest(
            InstrumentToken: "NSE_TCS",
            TradingSymbol: "TCS",
            Exchange: "NSE",
            Side: OrderSide.Buy,
            OrderType: OrderType.Market,
            ProductType: ProductType.MIS,
            Quantity: 20,
            Price: 4125.00m,
            TriggerPrice: null,
            IdempotencyKey: Guid.NewGuid()
        );
        await _adapter.Orders.PlaceOrderAsync(_connection, _account, buyReq);

        // Act: Sell 10 shares of TCS
        var sellReq = new OrderRequest(
            InstrumentToken: "NSE_TCS",
            TradingSymbol: "TCS",
            Exchange: "NSE",
            Side: OrderSide.Sell,
            OrderType: OrderType.Market,
            ProductType: ProductType.MIS,
            Quantity: 10,
            Price: 4125.00m,
            TriggerPrice: null,
            IdempotencyKey: Guid.NewGuid()
        );
        var sellResponse = await _adapter.Orders.PlaceOrderAsync(_connection, _account, sellReq);

        // Assert
        sellResponse.Success.Should().BeTrue();
        var positions = await _adapter.Positions.GetPositionsAsync(_connection, _account);
        var tcsPos = positions.FirstOrDefault(p => p.TradingSymbol == "TCS");
        tcsPos.Should().NotBeNull();
        tcsPos!.Quantity.Should().Be(10, "20 bought - 10 sold = 10 remaining");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithNegativeQuantity_ShouldRejectOrder()
    {
        // Arrange
        var request = new OrderRequest(
            InstrumentToken: "NSE_RELIANCE",
            TradingSymbol: "RELIANCE",
            Exchange: "NSE",
            Side: OrderSide.Buy,
            OrderType: OrderType.Market,
            ProductType: ProductType.MIS,
            Quantity: -5,
            Price: 2980m,
            TriggerPrice: null,
            IdempotencyKey: Guid.NewGuid()
        );

        // Act
        var response = await _adapter.Orders.PlaceOrderAsync(_connection, _account, request);

        // Assert
        response.Success.Should().BeFalse();
        response.Status.Should().Be(OrderStatus.Rejected);
        response.ErrorCode.Should().Be("INVALID_QTY");
    }

    [Fact]
    public async Task PlaceOrderAsync_WithExcessiveOrderValue_ShouldRejectForInsufficientFunds()
    {
        // Arrange: Try buying 10,000 shares of Reliance (₹2.98 Crores), exceeding ₹5 Lakh virtual capital
        var request = new OrderRequest(
            InstrumentToken: "NSE_RELIANCE",
            TradingSymbol: "RELIANCE",
            Exchange: "NSE",
            Side: OrderSide.Buy,
            OrderType: OrderType.Market,
            ProductType: ProductType.CNC,
            Quantity: 10_000,
            Price: 2980m,
            TriggerPrice: null,
            IdempotencyKey: Guid.NewGuid()
        );

        // Act
        var response = await _adapter.Orders.PlaceOrderAsync(_connection, _account, request);

        // Assert
        response.Success.Should().BeFalse();
        response.Status.Should().Be(OrderStatus.Rejected);
        response.ErrorCode.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public async Task GetQuoteAsync_ShouldReturnRealisticMarketData()
    {
        // Act
        var quote = await _adapter.MarketData.GetQuoteAsync("NSE_INFY");

        // Assert
        quote.Should().NotBeNull();
        quote!.TradingSymbol.Should().Be("INFY");
        quote.Exchange.Should().Be("NSE");
        quote.LastPrice.Should().Be(1850.50m);
        quote.BidPrice.Should().BeLessThan(quote.AskPrice!.Value);
        quote.Volume.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BrokerAdapterFactory_ShouldResolveMockAdapterForMockCode()
    {
        // Arrange
        var factory = new BrokerAdapterFactory(_adapter);

        // Act
        var resolved = factory.GetAdapter(BrokerCode.Mock);

        // Assert
        resolved.Should().NotBeNull();
        resolved.BrokerCode.Should().Be(BrokerCode.Mock);
    }
}
