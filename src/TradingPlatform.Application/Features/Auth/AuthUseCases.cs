using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Contracts.Auth;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Application.Features.Auth;

public record LoginUserCommand(LoginRequest Request) : IRequest<AuthResponse>;

public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Request.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public class LoginUserHandler(
    ITradingDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account has been deactivated.");
        }

        bool validPassword = passwordHasher.VerifyPassword(req.Password, user.PasswordHash);
        if (!validPassword)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Timezone, roles);
        return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(30), userDto);
    }
}

public record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<AuthResponse>;

public class RefreshTokenHandler(
    ITradingDbContext dbContext,
    ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var tokenStr = command.Request.RefreshToken;

        var existingToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == tokenStr, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existingToken.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is expired or has been revoked.");
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is deactivated.");
        }

        // Rotate token
        var newRefreshToken = tokenService.GenerateRefreshToken();
        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshToken;

        var newTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        dbContext.RefreshTokens.Add(newTokenEntity);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = tokenService.GenerateAccessToken(user, roles);

        await dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Timezone, roles);
        return new AuthResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(30), userDto);
    }
}

public record RevokeTokenCommand(RevokeTokenRequest Request) : IRequest<bool>;

public class RevokeTokenHandler(ITradingDbContext dbContext) : IRequestHandler<RevokeTokenCommand, bool>
{
    public async Task<bool> Handle(RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == command.Request.RefreshToken, cancellationToken);

        if (token == null || !token.IsActive)
            return false;

        token.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserHandler(
    ITradingDbContext dbContext,
    ICurrentUserService currentUserService) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == currentUserService.UserId.Value, cancellationToken)
            ?? throw new KeyNotFoundException("Authenticated user not found.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return new UserDto(user.Id, user.Email, user.FullName, user.Timezone, roles);
    }
}
