namespace CodeExplainer.Desktop.Models;

public record ApiResult(bool Success, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResult Ok(string? message = null) => new(true, message, null);
    public static ApiResult Fail(string? message, IReadOnlyList<string>? errors = null) => new(false, message, errors);
}

public sealed record ApiResult<T>(bool Success, T? Data, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResult<T> Ok(T? data, string? message = null)
        => new(true, data, message, null);

    public static ApiResult<T> Fail(string? message = null, IReadOnlyList<string>? errors = null)
        => new(false, default, message, errors);
}
