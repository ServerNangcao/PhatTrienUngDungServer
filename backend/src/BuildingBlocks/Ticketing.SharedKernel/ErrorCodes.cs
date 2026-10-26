namespace Ticketing.SharedKernel;

/// <summary>
/// Các mã lỗi chuẩn hóa dùng trong toàn bộ hệ thống (SharedKernel).
/// Được ánh xạ sang HTTP Status Code RFC 7807 trong ResultExtensions.
/// </summary>
public static class ErrorCodes
{
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Gone = "GONE";
    public const string Validation = "VALIDATION";
    public const string InternalError = "INTERNAL_ERROR";
}
