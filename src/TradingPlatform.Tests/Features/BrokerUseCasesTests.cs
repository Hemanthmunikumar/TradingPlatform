using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Features.Brokers;
using TradingPlatform.Contracts.Brokers;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;
using TradingPlatform.Infrastructure.Persistence;
using TradingPlatform.Infrastructure.Security;

namespace TradingPlatform.Tests.Features;

public class BrokerUseCasesTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly ICredentialEncryptionService _encryptionService;

    public BrokerUseCasesTests()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Security:MasterEncryptionKey"] = "MasterKeyForBrokerUseCasesUnitTests2026!"
        };
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        _encryptionService = new Aes256GcmEncryptionService(config);
    }

    private TradingDbContext CreateContext(string dbName)
    {
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns(_userId);
        mockUser.Setup(u => u.IsAdmin).Returns(false);
        mockUser.Setup(u => u.IsAuthenticated).Returns(true);

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TradingDbContext(options, mockUser.Object);
    }

    [Fact]
    public async Task CreateBrokerConnection_ShouldEncryptCredentialsAndMaskInResponse()
    {
        // Arrange
        var dbName = $"BrokerTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);

        var provider = new BrokerProvider
        {
            Id = Guid.NewGuid(),
            Name = "Upstox",
            Code = BrokerCode.Upstox,
            ApiBaseUrl = "https://api.upstox.com/v2"
        };
        context.BrokerProviders.Add(provider);
        await context.SaveChangesAsync();

        var handler = new CreateBrokerConnectionHandler(context, _encryptionService);
        var request = new CreateBrokerConnectionRequest(
            BrokerProviderId: provider.Id,
            DisplayName: "My Upstox Pro",
            ApiKey: "UPSTOX_KEY_7788",
            ApiSecret: "SECRET_VALUE_9900",
            TradingMode: TradingMode.Paper
        );

        // Act
        var result = await handler.Handle(new CreateBrokerConnectionCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("My Upstox Pro");
        result.MaskedApiKey.Should().Be("***7788");

        // Verify database persistence has encrypted credentials
        var savedConnection = await context.BrokerConnections
            .Include(bc => bc.TradingAccounts)
                .ThenInclude(ta => ta.Configuration)
            .FirstAsync(bc => bc.Id == result.Id);

        savedConnection.EncryptedApiKey.Should().NotBe("UPSTOX_KEY_7788");
        savedConnection.EncryptedApiKey.Should().Contain(":"); // GCM multi-part format
        _encryptionService.Decrypt(savedConnection.EncryptedApiKey).Should().Be("UPSTOX_KEY_7788");
        _encryptionService.Decrypt(savedConnection.EncryptedApiSecret).Should().Be("SECRET_VALUE_9900");

        // Verify linked default TradingAccount and 2-second approval configuration
        savedConnection.TradingAccounts.Should().ContainSingle();
        var account = savedConnection.TradingAccounts.First();
        account.Configuration.Should().NotBeNull();
        account.Configuration!.ApprovalDurationSeconds.Should().Be(2);
        account.Configuration.AutoExecuteOnTimeout.Should().BeTrue("Paper mode defaults auto-execute to true");
    }

    [Fact]
    public async Task ToggleTradingMode_WhenSwitchingToLive_ShouldDisableAutoExecuteByDefault()
    {
        // Arrange
        var dbName = $"BrokerTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);

        var provider = new BrokerProvider { Id = Guid.NewGuid(), Name = "Mock", Code = BrokerCode.Mock, ApiBaseUrl = "http://localhost" };
        var connection = new BrokerConnection
        {
            BrokerProvider = provider,
            DisplayName = "Test Mode Switch",
            EncryptedApiKey = _encryptionService.Encrypt("KEY1234"),
            EncryptedApiSecret = _encryptionService.Encrypt("SECRET123"),
            TradingMode = TradingMode.Paper
        };

        var account = new TradingAccount
        {
            BrokerConnection = connection,
            AccountId = "ACC-01",
            AccountName = "Primary Account",
            TradingMode = TradingMode.Paper,
            Configuration = new AccountConfiguration
            {
                ApprovalDurationSeconds = 2,
                AutoExecuteOnTimeout = true // Initially true in paper mode
            }
        };

        connection.TradingAccounts.Add(account);
        context.BrokerConnections.Add(connection);
        await context.SaveChangesAsync();

        var handler = new ToggleTradingModeHandler(context, _encryptionService);

        // Act: Switch to Live mode
        var result = await handler.Handle(new ToggleTradingModeCommand(connection.Id, TradingMode.Live), CancellationToken.None);

        // Assert
        result.TradingMode.Should().Be(TradingMode.Live);

        var updatedAccount = await context.TradingAccounts
            .Include(ta => ta.Configuration)
            .FirstAsync(ta => ta.Id == account.Id);

        updatedAccount.TradingMode.Should().Be(TradingMode.Live);
        updatedAccount.Configuration!.AutoExecuteOnTimeout.Should().BeFalse("Live mode must disable auto-execution on timeout by default for safety");
    }

    [Fact]
    public async Task UpdateAccountConfiguration_ShouldUpdateRiskLimitsAndApprovalWindow()
    {
        // Arrange
        var dbName = $"BrokerTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);

        var account = new TradingAccount
        {
            AccountId = "ACC-99",
            AccountName = "Risk Test Account",
            Configuration = new AccountConfiguration
            {
                MaxCapitalAllocation = 50_000m,
                ApprovalDurationSeconds = 2
            }
        };
        context.TradingAccounts.Add(account);
        await context.SaveChangesAsync();

        var handler = new UpdateAccountConfigurationHandler(context);
        var request = new UpdateAccountConfigurationRequest(
            MaxCapitalAllocation: 250_000m,
            MaxOrderValue: 50_000m,
            MaxPositions: 8,
            MaxRiskPerTradePercent: 1.5m,
            DailyLossLimit: 10_000m,
            MaxExposurePerStockPercent: 25m,
            MaxTradesPerDay: 15,
            StopLossRequired: true,
            ApprovalDurationSeconds: 5, // user configured to 5 seconds
            AutoExecuteOnTimeout: false,
            KillSwitchActive: false
        );

        // Act
        var result = await handler.Handle(new UpdateAccountConfigurationCommand(account.Id, request), CancellationToken.None);

        // Assert
        result.MaxCapitalAllocation.Should().Be(250_000m);
        result.ApprovalDurationSeconds.Should().Be(5);
        result.MaxPositions.Should().Be(8);
        result.DailyLossLimit.Should().Be(10_000m);
    }
}
