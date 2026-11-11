using FluentAssertions;
using TradingPlatform.Infrastructure.Security;

namespace TradingPlatform.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldGenerateFormattedHashWithSalt()
    {
        // Act
        var hash = _hasher.HashPassword("SecureTradePass@2026");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        var parts = hash.Split('.');
        parts.Should().HaveCount(3, "Hash format must be {iterations}.{salt}.{hash}");
        parts[0].Should().Be("100000");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "AlgoTrader#9988";
        var hash = _hasher.HashPassword(password);

        // Act
        bool isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("CorrectPassword123");

        // Act
        bool isValid = _hasher.VerifyPassword("WrongPassword456", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldGenerateDifferentSalts()
    {
        // Arrange
        const string password = "ConsistentPassword";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2, "Each hash must utilize a distinct cryptographically random salt");
        _hasher.VerifyPassword(password, hash1).Should().BeTrue();
        _hasher.VerifyPassword(password, hash2).Should().BeTrue();
    }
}
