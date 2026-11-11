using TradingPlatform.Contracts.Orders;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Application.Interfaces.Orders;

public interface IIndianBrokerageCalculator
{
    IndianBrokerageChargesDto CalculateSingleLegCharges(
        OrderSide side,
        ProductType productType,
        decimal price,
        int quantity,
        string exchange = "NSE");

    IndianBrokerageChargesDto CalculateRoundTripCharges(
        ProductType productType,
        decimal buyPrice,
        decimal sellPrice,
        int quantity,
        string exchange = "NSE");
}
