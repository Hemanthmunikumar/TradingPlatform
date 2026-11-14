using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Features.Auth;
using TradingPlatform.Contracts.Auth;
using TradingPlatform.Infrastructure.Persistence;
using Xunit;

namespace TradingPlatform.Tests.Security;

public class UserRegistrationTests
{
    private TradingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserRegTest_{Guid.NewGuid()}")
            .Options;

        var mockCurrentUserService = new Mock<ICurrentUserService>();
        return new TradingDbContext(options, mockCurrentUserService.Object);
    }

    [Fact]
    public async Task RegisterUser_WhenDatabaseIsEmpty_ShouldSeedDefaultRoleAndRegisterSuccessfully()
    {
        using var db = CreateDbContext();

        var mockPasswordHasher = new Mock<IPasswordHasher>();
        mockPasswordHasher.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed_secure_pass");

        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(t => t.GenerateAccessToken(It.IsAny<TradingPlatform.Domain.Entities.User>(), It.IsAny<IEnumerable<string>>())).Returns("dummy_access_token");
        mockTokenService.Setup(t => t.GenerateRefreshToken()).Returns("dummy_refresh_token");

        var handler = new RegisterUserHandler(db, mockPasswordHasher.Object, mockTokenService.Object);

        var command = new RegisterUserCommand(new RegisterRequest(
            Email: "trader@example.com",
            Password: "SecurePassword123!",
            FullName: "Pro Trader",
            PhoneNumber: null,
            Timezone: "Asia/Kolkata"
        ));

        var response = await handler.Handle(command, CancellationToken.None);

        response.Should().NotBeNull();
        response.AccessToken.Should().Be("dummy_access_token");
        response.RefreshToken.Should().Be("dummy_refresh_token");
        response.User.Email.Should().Be("trader@example.com");
        response.User.FullName.Should().Be("Pro Trader");

        var persisted = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == "trader@example.com");
        persisted.Should().NotBeNull();
        persisted!.UserRoles.Should().HaveCount(1);
        persisted.UserRoles.First().Role.Name.Should().Be("User");
    }
}
