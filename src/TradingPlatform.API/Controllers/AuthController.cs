using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TradingPlatform.Application.Common;
using TradingPlatform.Application.Features.Auth;
using TradingPlatform.Application.Interfaces.Notifications;
using TradingPlatform.Contracts.Auth;
using TradingPlatform.Contracts.Common;
using TradingPlatform.Contracts.Notifications;

namespace TradingPlatform.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    ISender sender,
    ITradingDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEmailService emailService,
    IMemoryCache memoryCache) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new RegisterUserCommand(request), cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "User registered successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new LoginUserCommand(request), cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new RefreshTokenCommand(request), cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed successfully."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("revoke-token")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeTokenCommand(request), cancellationToken);
        return Ok(ApiResponse<bool>.Ok(result, result ? "Token revoked." : "Token not found or already inactive."));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
            return Ok(ApiResponse<UserDto>.Ok(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<object>.Fail("Email address is required."));
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive, cancellationToken);

        if (user != null)
        {
            var otp = Random.Shared.Next(100000, 999999).ToString();
            memoryCache.Set($"pwd_reset:{email}", otp, TimeSpan.FromMinutes(15));

            var html = $@"
                <div style='font-family:Arial,sans-serif;background:#0f172a;color:#f8fafc;padding:24px;border-radius:12px;'>
                    <h2 style='color:#38bdf8;margin-top:0;'>StrikeSutra Password Reset</h2>
                    <p>Hello {user.FullName},</p>
                    <p>Use this 6-digit verification code to reset your StrikeSutra password (valid for 15 minutes):</p>
                    <div style='font-size:28px;font-weight:bold;letter-spacing:6px;background:#1e293b;padding:14px 20px;border-radius:8px;display:inline-block;color:#34d399;'>{otp}</div>
                    <p style='color:#94a3b8;font-size:12px;margin-top:20px;'>If you did not request this reset, you can safely ignore this email.</p>
                </div>";

            await emailService.SendEmailAsync(new EmailMessageDto(
                ToEmail: user.Email,
                Subject: "StrikeSutra — Password Reset Verification Code",
                HtmlBody: html
            ), cancellationToken);
        }

        return Ok(ApiResponse<bool>.Ok(true, "If an account exists for that email, a 6-digit reset code has been sent."));
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(ApiResponse<object>.Fail("Email, reset code, and new password are required."));
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(ApiResponse<object>.Fail("New password must be at least 8 characters."));
        }

        if (!memoryCache.TryGetValue($"pwd_reset:{email}", out string? expectedCode) || expectedCode != request.Code.Trim())
        {
            return BadRequest(ApiResponse<object>.Fail("Invalid or expired reset code."));
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive, cancellationToken);

        if (user == null)
        {
            return BadRequest(ApiResponse<object>.Fail("Account not found."));
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        memoryCache.Remove($"pwd_reset:{email}");

        return Ok(ApiResponse<bool>.Ok(true, "Password updated successfully. You can now sign in."));
    }
}
