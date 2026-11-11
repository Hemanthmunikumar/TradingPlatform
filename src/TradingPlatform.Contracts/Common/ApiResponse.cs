namespace TradingPlatform.Contracts.Common;

/// <summary>
/// Standard unified API response wrapper.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error, string? message = null) =>
        new() { Success = false, Errors = [error], Message = message };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Errors = errors, Message = message };
}
