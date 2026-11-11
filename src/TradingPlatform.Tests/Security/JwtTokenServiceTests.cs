using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TradingPlatform.Domain.Entities;
using TradingPlatform.Infrastructure.Security;

namespace TradingPlatform.Tests.Security;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "UnitTestingSuperSecretKeyWithAtLeast32BytesLength!",
            ["Jwt:Issuer"] = "TradingPlatform.TestApi",
            ["Jwt:Audience"] = "TradingPlatform.TestClient",
            ["Jwt:AccessTokenExpirationMinutes"] = "15"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new JwtTokenService(configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidJwtWithExpectedClaims()
    {
        // Arrange
        var user = new User
        {
            Email = "trader@example.com",
            FullName = "Arjun Mehta",
            Timezone = "Asia/Kolkata"
        };
        var roles = new[] { "User", "Trader" };

        // Act
        var token = _tokenService.GenerateAccessToken(user, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var principal = _tokenService.GetPrincipalFromToken(token, validateLifetime: true);
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Email)?.Value.Should().Be("trader@example.com");
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be("Arjun Mehta");
        principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Should().Contain(["User", "Trader"]);
        principal.FindFirst("timezone")?.Value.Should().Be("Asia/Kolkata");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceCryptographicallyRandomBase64String()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
        token1.Length.Should().BeGreaterThan(64);
    }

    [Fact]
    public void GetPrincipalFromToken_WithTamperedSignature_ShouldReturnNull()
    {
        // Arrange
        var user = new User { Email = "tamper@example.com", FullName = "Tamper Test" };
        var validToken = _tokenService.GenerateAccessToken(user, ["User"]);

        // Tamper with the token string
        var tamperedToken = validToken[..^4] + "abcd";

        // Act
        var principal = _tokenService.GetPrincipalFromToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }
}
