using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Interfaces.Adapters;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Adapters;
using TradingPlatform.Infrastructure.Adapters.Mock;
using TradingPlatform.Infrastructure.Adapters.Upstox;
using TradingPlatform.Infrastructure.Adapters.Zerodha;
using Xunit;

namespace TradingPlatform.Tests.Adapters;

public class BrokerLiveAdaptersTests
{
    [Fact]
    public void BrokerAdapterFactory_ResolvesAllSupportedBrokersCorrectly()
    {
        var mockEncryption = new Mock<ICredentialEncryptionService>();
        mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns("dummy_secret");

        var mockLoggerUpstox = new Mock<ILogger<UpstoxBrokerAdapter>>();
        var mockLoggerZerodha = new Mock<ILogger<ZerodhaBrokerAdapter>>();

        var mockAdapter = new MockBrokerAdapter();
        var upstoxAdapter = new UpstoxBrokerAdapter(new HttpClient(), mockEncryption.Object, mockLoggerUpstox.Object);
        var zerodhaAdapter = new ZerodhaBrokerAdapter(new HttpClient(), mockEncryption.Object, mockLoggerZerodha.Object);

        var factory = new BrokerAdapterFactory(mockAdapter, upstoxAdapter, zerodhaAdapter);

        factory.GetAdapter(BrokerCode.Mock).Should().BeSameAs(mockAdapter);
        factory.GetAdapter(BrokerCode.Upstox).Should().BeSameAs(upstoxAdapter);
        factory.GetAdapter(BrokerCode.Zerodha).Should().BeSameAs(zerodhaAdapter);
    }

    [Fact]
    public async Task UpstoxAdapter_GeneratesValidOAuthUrl()
    {
        var mockEncryption = new Mock<ICredentialEncryptionService>();
        mockEncryption.Setup(e => e.Decrypt("ENC_KEY")).Returns("my_upstox_api_key");

        var adapter = new UpstoxBrokerAdapter(new HttpClient(), mockEncryption.Object, new Mock<ILogger<UpstoxBrokerAdapter>>().Object);
        var conn = new BrokerConnection { EncryptedApiKey = "ENC_KEY" };

        var url = await adapter.GetAuthorizationUrlAsync(conn, "http://localhost/callback", "state123");

        url.Should().Contain("api.upstox.com/v2/login/authorization/dialog");
        url.Should().Contain("client_id=my_upstox_api_key");
        url.Should().Contain("state=state123");
    }

    [Fact]
    public async Task ZerodhaAdapter_GeneratesValidOAuthUrl()
    {
        var mockEncryption = new Mock<ICredentialEncryptionService>();
        mockEncryption.Setup(e => e.Decrypt("ENC_KEY")).Returns("my_zerodha_api_key");

        var adapter = new ZerodhaBrokerAdapter(new HttpClient(), mockEncryption.Object, new Mock<ILogger<ZerodhaBrokerAdapter>>().Object);
        var conn = new BrokerConnection { EncryptedApiKey = "ENC_KEY" };

        var url = await adapter.GetAuthorizationUrlAsync(conn, "http://localhost/callback", "state123");

        url.Should().Contain("kite.zerodha.com/connect/login");
        url.Should().Contain("api_key=my_zerodha_api_key");
    }

    [Fact]
    public async Task LiveBrokers_InPaperMode_NeverCallLiveEndpoints()
    {
        var mockEncryption = new Mock<ICredentialEncryptionService>();
        var upstox = new UpstoxBrokerAdapter(new HttpClient(), mockEncryption.Object, new Mock<ILogger<UpstoxBrokerAdapter>>().Object);
        var zerodha = new ZerodhaBrokerAdapter(new HttpClient(), mockEncryption.Object, new Mock<ILogger<ZerodhaBrokerAdapter>>().Object);

        var conn = new BrokerConnection();
        var paperAccount = new TradingAccount { TradingMode = TradingMode.Paper, AccountId = "PAPER_01" };
        var orderReq = new OrderRequest("INFY", "INFY", "NSE", OrderSide.Buy, OrderType.Market, ProductType.MIS, 10, 1850m, null, Guid.NewGuid());

        var upstoxRes = await upstox.Orders.PlaceOrderAsync(conn, paperAccount, orderReq);
        upstoxRes.Success.Should().BeTrue();
        upstoxRes.BrokerOrderId.Should().StartWith("PAPER-UPSTOX-");

        var zerodhaRes = await zerodha.Orders.PlaceOrderAsync(conn, paperAccount, orderReq);
        zerodhaRes.Success.Should().BeTrue();
        zerodhaRes.BrokerOrderId.Should().StartWith("PAPER-ZERODHA-");
    }
}
