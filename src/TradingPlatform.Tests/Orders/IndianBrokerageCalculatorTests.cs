using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Orders;
using Xunit;

namespace TradingPlatform.Tests.Orders;

public class IndianBrokerageCalculatorTests
{
    private readonly IndianBrokerageCalculator _calculator = new();

    [Fact]
    public void CalculateSingleLegCharges_MisBuy_CalculatesAccurately()
    {
        // 100 shares @ ₹1,000 = Turnover: ₹100,000
        var charges = _calculator.CalculateSingleLegCharges(
            side: OrderSide.Buy,
            productType: ProductType.MIS,
            price: 1000m,
            quantity: 100,
            exchange: "NSE"
        );

        // Brokerage: 0.05% of 100,000 = ₹50, capped at ₹20
        Assert.Equal(20.00m, charges.Brokerage);

        // STT: 0% on MIS Buy
        Assert.Equal(0.00m, charges.Stt);

        // Exchange charges: 0.00325% of 100,000 = ₹3.25
        Assert.Equal(3.25m, charges.ExchangeCharges);

        // SEBI fees: ₹10 per crore = 0.0001% of 100,000 = ₹0.10
        Assert.Equal(0.10m, charges.SebiFees);

        // GST: 18% on (20.00 + 3.25 + 0.10 = 23.35) = ₹4.20
        Assert.Equal(4.20m, charges.Gst);

        // Stamp Duty: 0.003% of 100,000 = ₹3.00
        Assert.Equal(3.00m, charges.StampDuty);

        // DP Charges: ₹0 for MIS
        Assert.Equal(0.00m, charges.DpCharges);

        // Total = 20.00 + 0 + 3.25 + 0.10 + 4.20 + 3.00 + 0 = 30.55
        Assert.Equal(30.55m, charges.TotalCharges);
    }

    [Fact]
    public void CalculateSingleLegCharges_MisSell_IncludesStt()
    {
        // 100 shares @ ₹1,050 = Turnover: ₹105,000
        var charges = _calculator.CalculateSingleLegCharges(
            side: OrderSide.Sell,
            productType: ProductType.MIS,
            price: 1050m,
            quantity: 100,
            exchange: "NSE"
        );

        // Brokerage capped at ₹20
        Assert.Equal(20.00m, charges.Brokerage);

        // STT: 0.025% of 105,000 = ₹26.25
        Assert.Equal(26.25m, charges.Stt);

        // Stamp duty is 0 on Sell
        Assert.Equal(0.00m, charges.StampDuty);

        Assert.True(charges.TotalCharges > charges.Brokerage + charges.Stt);
    }

    [Fact]
    public void CalculateSingleLegCharges_CncDelivery_ZeroBrokerageAndIncludesDpOnSell()
    {
        var buyCharges = _calculator.CalculateSingleLegCharges(
            side: OrderSide.Buy,
            productType: ProductType.CNC,
            price: 500m,
            quantity: 50,
            exchange: "NSE"
        );

        Assert.Equal(0m, buyCharges.Brokerage); // Free delivery brokerage
        Assert.Equal(0m, buyCharges.DpCharges); // No DP charge on buy
        Assert.True(buyCharges.StampDuty > 0);  // 0.015% stamp duty on buy

        var sellCharges = _calculator.CalculateSingleLegCharges(
            side: OrderSide.Sell,
            productType: ProductType.CNC,
            price: 550m,
            quantity: 50,
            exchange: "NSE"
        );

        Assert.Equal(0m, sellCharges.Brokerage);
        Assert.Equal(15.93m, sellCharges.DpCharges); // ₹13.50 + 18% GST per scrip
        Assert.True(sellCharges.Stt > 0); // 0.1% STT on CNC Sell
    }

    [Fact]
    public void CalculateRoundTripCharges_SumsBothLegsCorrectly()
    {
        var roundTrip = _calculator.CalculateRoundTripCharges(
            productType: ProductType.MIS,
            buyPrice: 1000m,
            sellPrice: 1050m,
            quantity: 100,
            exchange: "NSE"
        );

        Assert.True(roundTrip.TotalCharges > 50m);
        Assert.True(roundTrip.Brokerage <= 40m); // ₹20 Buy + ₹20 Sell
        Assert.True(roundTrip.Stt > 0m); // Sell leg STT
    }
}
