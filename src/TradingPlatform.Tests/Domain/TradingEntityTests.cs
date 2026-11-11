using FluentAssertions;
using TradingPlatform.Domain.Common;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Domain.Enums;

namespace TradingPlatform.Tests.Domain;

public class TradingEntityTests
{
    [Fact]
    public void PendingTradeApproval_ShouldCalculateEstimatedOrderValueAccurately()
    {
        // Arrange
        var approval = new PendingTradeApproval
        {
            Symbol = "TCS",
            CompanyName = "Tata Consultancy Services",
            Side = OrderSide.Buy,
            Quantity = 25,
            IndicativePrice = 4120.50m,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddSeconds(2),
            Status = ApprovalStatus.Pending
        };

        // Act & Assert
        approval.EstimatedOrderValue.Should().Be(103012.50m);
        approval.IsExpired.Should().BeFalse();
        approval.IdempotencyKey.Should().NotBeEmpty();
    }

    [Fact]
    public void PendingTradeApproval_WhenDeadlinePassed_ShouldReportExpired()
    {
        // Arrange
        var approval = new PendingTradeApproval
        {
            Symbol = "INFY",
            Quantity = 10,
            IndicativePrice = 1850m,
            ApprovalDeadlineUtc = DateTime.UtcNow.AddSeconds(-1), // Past deadline
            Status = ApprovalStatus.Pending
        };

        // Act & Assert
        approval.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Position_UnrealizedPnL_ShouldCalculateAccurately()
    {
        // Arrange
        var position = new Position
        {
            Quantity = 100,
            AverageBuyPrice = 2500.00m,
            CurrentPrice = 2545.50m,
            IsOpen = true
        };

        // Act & Assert (Gain of 45.50 per share * 100 shares = 4550.00)
        position.UnrealizedPnL.Should().Be(4550.00m);
    }

    [Fact]
    public void Position_WhenClosed_UnrealizedPnL_ShouldBeZero()
    {
        // Arrange
        var position = new Position
        {
            Quantity = 0,
            AverageBuyPrice = 2500.00m,
            CurrentPrice = 2600.00m,
            IsOpen = false
        };

        // Act & Assert
        position.UnrealizedPnL.Should().Be(0m);
    }

    [Fact]
    public void RefreshToken_WhenExpiredOrRevoked_ShouldReportInactive()
    {
        // Arrange
        var activeToken = new RefreshToken
        {
            Token = "valid_token_xyz",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        var expiredToken = new RefreshToken
        {
            Token = "expired_token_xyz",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var revokedToken = new RefreshToken
        {
            Token = "revoked_token_xyz",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            RevokedAtUtc = DateTime.UtcNow
        };

        // Assert
        activeToken.IsActive.Should().BeTrue();
        expiredToken.IsActive.Should().BeFalse();
        revokedToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AllTenantEntities_MustImplementIMustHaveTenant()
    {
        // Arrange
        var tenantEntities = new object[]
        {
            new BrokerConnection(),
            new TradingAccount(),
            new AccountConfiguration(),
            new RiskProfile(),
            new StrategyDefinition(),
            new StrategyVersion(),
            new StrategyAccountMapping(),
            new TradingSignal(),
            new PendingTradeApproval(),
            new TradingOrder(),
            new TradeExecution(),
            new Position(),
            new Notification(),
            new DailyReport(),
            new AuditLog()
        };

        // Assert
        foreach (var entity in tenantEntities)
        {
            entity.Should().BeAssignableTo<IMustHaveTenant>($"{entity.GetType().Name} must implement IMustHaveTenant for multi-tenant isolation");
        }
    }
}
