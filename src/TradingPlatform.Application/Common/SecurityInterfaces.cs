using System.Security.Claims;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Application.Common;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime = false);
}
