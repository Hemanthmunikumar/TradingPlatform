using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Adapters.Upstox;
using TradingPlatform.Infrastructure.Adapters.Zerodha;

namespace TradingPlatform.Infrastructure.Adapters;

public class BrokerAdapterFactory(
    MockBrokerAdapter mockBrokerAdapter,
    UpstoxBrokerAdapter upstoxBrokerAdapter,
    ZerodhaBrokerAdapter zerodhaBrokerAdapter) : IBrokerAdapterFactory
{
    public IBrokerAdapter GetAdapter(BrokerCode brokerCode)
    {
        return brokerCode switch
        {
            BrokerCode.Mock => mockBrokerAdapter,
            BrokerCode.Upstox => upstoxBrokerAdapter,
            BrokerCode.Zerodha => zerodhaBrokerAdapter,
            _ => throw new NotSupportedException($"Broker provider '{brokerCode}' is not currently supported.")
        };
    }
}
