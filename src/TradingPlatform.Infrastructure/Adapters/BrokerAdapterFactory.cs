using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters.Mock;

namespace TradingPlatform.Infrastructure.Adapters;

public class BrokerAdapterFactory(MockBrokerAdapter mockBrokerAdapter) : IBrokerAdapterFactory
{
    private readonly MockBrokerAdapter _mockBrokerAdapter = mockBrokerAdapter;

    public IBrokerAdapter GetAdapter(BrokerCode brokerCode)
    {
        return brokerCode switch
        {
            BrokerCode.Mock => _mockBrokerAdapter,
            BrokerCode.Upstox => _mockBrokerAdapter, // Defaults to Mock adapter in paper mode until live credentials verified in M19
            BrokerCode.Zerodha => _mockBrokerAdapter,
            _ => throw new NotSupportedException($"Broker provider '{brokerCode}' is not currently supported.")
        };
    }
}
