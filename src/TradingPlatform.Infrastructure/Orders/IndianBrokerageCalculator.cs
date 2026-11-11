using TradingPlatform.Application.Interfaces.Orders;
using TradingPlatform.Contracts.Orders;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Infrastructure.Orders;

public class IndianBrokerageCalculator : IIndianBrokerageCalculator
{
    private const decimal MisBrokerageRate = 0.0005m; // 0.05%
    private const decimal MisMaxBrokeragePerOrder = 20.00m;
    private const decimal CncBrokerage = 0.00m; // Discount broker zero delivery brokerage

    private const decimal MisSttSellRate = 0.00025m; // 0.025% on sell side only
    private const decimal CncSttRate = 0.001m; // 0.1% on buy and sell

    private const decimal NseExchangeRate = 0.0000325m; // 0.00325%
    private const decimal BseExchangeRate = 0.0000375m; // 0.00375%

    private const decimal SebiRate = 0.000001m; // ₹10 per crore (0.0001%)
    private const decimal GstRate = 0.18m; // 18% on (brokerage + exchange charges + sebi)

    private const decimal MisStampDutyBuyRate = 0.00003m; // 0.003% on buy side only
    private const decimal CncStampDutyBuyRate = 0.00015m; // 0.015% on buy side only

    private const decimal CncDpChargesSell = 15.93m; // ₹13.50 + 18% GST per scrip sell

    public IndianBrokerageChargesDto CalculateSingleLegCharges(
        OrderSide side,
        ProductType productType,
        decimal price,
        int quantity,
        string exchange = "NSE")
    {
        var turnover = price * quantity;
        var exchangeRate = exchange.Equals("BSE", StringComparison.OrdinalIgnoreCase) ? BseExchangeRate : NseExchangeRate;

        // Brokerage
        decimal brokerage = 0m;
        if (productType == ProductType.MIS)
        {
            brokerage = Math.Min(MisMaxBrokeragePerOrder, Math.Round(turnover * MisBrokerageRate, 2));
        }
        else
        {
            brokerage = CncBrokerage;
        }

        // STT
        decimal stt = 0m;
        if (productType == ProductType.MIS)
        {
            if (side == OrderSide.Sell)
            {
                stt = Math.Round(turnover * MisSttSellRate, 2);
            }
        }
        else
        {
            stt = Math.Round(turnover * CncSttRate, 2);
        }

        // Exchange Transaction Charges
        var exchangeCharges = Math.Round(turnover * exchangeRate, 2);

        // SEBI Turnover Fees
        var sebiFees = Math.Round(turnover * SebiRate, 2);

        // GST
        var gst = Math.Round((brokerage + exchangeCharges + sebiFees) * GstRate, 2);

        // Stamp Duty (Buy side only)
        decimal stampDuty = 0m;
        if (side == OrderSide.Buy)
        {
            var stampRate = productType == ProductType.MIS ? MisStampDutyBuyRate : CncStampDutyBuyRate;
            stampDuty = Math.Round(turnover * stampRate, 2);
        }

        // DP Charges (CNC Sell only)
        decimal dpCharges = 0m;
        if (productType == ProductType.CNC && side == OrderSide.Sell)
        {
            dpCharges = CncDpChargesSell;
        }

        var totalCharges = brokerage + stt + exchangeCharges + gst + sebiFees + stampDuty + dpCharges;

        return new IndianBrokerageChargesDto(
            Brokerage: brokerage,
            Stt: stt,
            ExchangeCharges: exchangeCharges,
            Gst: gst,
            SebiFees: sebiFees,
            StampDuty: stampDuty,
            DpCharges: dpCharges,
            TotalCharges: totalCharges
        );
    }

    public IndianBrokerageChargesDto CalculateRoundTripCharges(
        ProductType productType,
        decimal buyPrice,
        decimal sellPrice,
        int quantity,
        string exchange = "NSE")
    {
        var buyCharges = CalculateSingleLegCharges(OrderSide.Buy, productType, buyPrice, quantity, exchange);
        var sellCharges = CalculateSingleLegCharges(OrderSide.Sell, productType, sellPrice, quantity, exchange);

        return new IndianBrokerageChargesDto(
            Brokerage: buyCharges.Brokerage + sellCharges.Brokerage,
            Stt: buyCharges.Stt + sellCharges.Stt,
            ExchangeCharges: buyCharges.ExchangeCharges + sellCharges.ExchangeCharges,
            Gst: buyCharges.Gst + sellCharges.Gst,
            SebiFees: buyCharges.SebiFees + sellCharges.SebiFees,
            StampDuty: buyCharges.StampDuty + sellCharges.StampDuty,
            DpCharges: buyCharges.DpCharges + sellCharges.DpCharges,
            TotalCharges: buyCharges.TotalCharges + sellCharges.TotalCharges
        );
    }
}
