using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Application.Common;
using TradingPlatform.Contracts.Auth;
using TradingPlatform.Domain.Entities;

namespace TradingPlatform.Application.Features.Auth;

public record RegisterUserCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(x => x.Request.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(128).WithMessage("Full name cannot exceed 128 characters.");
    }
}

public class RegisterUserHandler(
    ITradingDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        // Verify email uniqueness
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        bool exists = await dbContext.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"User with email '{req.Email}' already exists.");
        }

        // Get or create default User role
        var defaultRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
        if (defaultRole == null)
        {
            defaultRole = new Role
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "User",
                Description = "Standard Trading User"
            };
            dbContext.Roles.Add(defaultRole);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var user = new User
        {
            Email = normalizedEmail,
            FullName = req.FullName.Trim(),
            PhoneNumber = req.PhoneNumber?.Trim(),
            Timezone = string.IsNullOrWhiteSpace(req.Timezone) ? "Asia/Kolkata" : req.Timezone.Trim(),
            PasswordHash = passwordHasher.HashPassword(req.Password),
            IsActive = true
        };

        user.UserRoles.Add(new UserRole { User = user, Role = defaultRole });
        dbContext.Users.Add(user);

        // Generate tokens
        var roles = new[] { defaultRole.Name };
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FullName, user.Timezone, roles);
        return new AuthResponse(accessToken, refreshToken, DateTime.UtcNow.AddMinutes(30), userDto);
    }
}
