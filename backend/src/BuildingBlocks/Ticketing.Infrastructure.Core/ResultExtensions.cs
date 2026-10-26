using Microsoft.AspNetCore.Http;
using Ticketing.SharedKernel;

namespace Ticketing.Infrastructure.Core;

/// <summary>
/// Extension methods để chuyển Result thành HTTP response (ProblemDetails).
/// Dùng trong Minimal API endpoints.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Chuyển Result&lt;T&gt; thành IResult cho Minimal API.
    /// Thành công → 200 OK với giá trị.
    /// Thất bại → ProblemDetails với status code phù hợp.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, int successStatusCode = 200)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                201 => Results.Created(string.Empty, result.Value),
                204 => Results.NoContent(),
                _ => Results.Ok(result.Value),
            };
        }

        return ToProblemResult(result);
    }

    /// <summary>
    /// Chuyển Result thành IResult (không có giá trị trả về).
    /// </summary>
    public static IResult ToHttpResult(this Result result, int successStatusCode = 204)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                200 => Results.Ok(),
                204 => Results.NoContent(),
                _ => Results.Ok(),
            };
        }

        return ToProblemResult(result);
    }

    private static IResult ToProblemResult(Result result)
    {
        var statusCode = result.ErrorCode switch
        {
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.Gone => StatusCodes.Status410Gone,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: GetTitle(statusCode),
            detail: result.Error,
            statusCode: statusCode);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        410 => "Gone",
        422 => "Validation Error",
        _ => "Error",
    };
}
