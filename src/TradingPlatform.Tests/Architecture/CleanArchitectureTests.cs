using FluentAssertions;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Domain.Common;

namespace TradingPlatform.Tests.Architecture;

public class CleanArchitectureTests
{
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    private record TestDomainEvent(string Message, DateTime OccurredOnUtc) : IDomainEvent;

    [Fact]
    public void BaseEntity_ShouldInitializeWithUniqueIdAndUtcTimestamp()
    {
        // Act
        var entity = new TestEntity { Name = "Nifty50" };

        // Assert
        entity.Id.Should().NotBeEmpty();
        entity.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        entity.UpdatedAtUtc.Should().BeNull();
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void BaseEntity_ShouldCollectAndClearDomainEvents()
    {
        // Arrange
        var entity = new TestEntity { Name = "BankNifty" };
        var domainEvent = new TestDomainEvent("Symbol registered", DateTime.UtcNow);

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);

        // Act 2
        entity.ClearDomainEvents();

        // Assert 2
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ApiResponse_Ok_ShouldConstructSuccessPayload()
    {
        // Arrange & Act
        var response = ApiResponse<string>.Ok("Operational", "All systems green");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be("Operational");
        response.Message.Should().Be("All systems green");
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_ShouldConstructFailurePayload()
    {
        // Arrange & Act
        var response = ApiResponse<string>.Fail("Invalid credentials", "Authentication error");

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Errors.Should().ContainSingle().Which.Should().Be("Invalid credentials");
        response.Message.Should().Be("Authentication error");
    }
}
