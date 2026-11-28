namespace TradingPlatform.Contracts.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    string? Timezone = "Asia/Kolkata"
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record RevokeTokenRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Timezone,
    IEnumerable<string> Roles
);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Code, string NewPassword);
