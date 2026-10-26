using Ticketing.SharedKernel;
using Xunit;

namespace Integration.Tests;

public class ResultTests
{
    [Fact]
    public void Success_Creates_Valid_Success_Result()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Failure_Creates_Valid_Failure_Result()
    {
        var result = Result.Failure("Something went wrong", ErrorCodes.BadRequest);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
        Assert.Equal(ErrorCodes.BadRequest, result.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failure_With_Null_Or_Whitespace_Error_Throws_InvalidOperationException(string? invalidError)
    {
        Assert.Throws<InvalidOperationException>(() => Result.Failure(invalidError!));
        Assert.Throws<InvalidOperationException>(() => Result.Failure<int>(invalidError!));
    }

    [Fact]
    public void Generic_Result_Success_Returns_Value_And_Throws_On_Failure_Access()
    {
        var successResult = Result.Success(42);

        Assert.True(successResult.IsSuccess);
        Assert.False(successResult.IsFailure);
        Assert.Equal(42, successResult.Value);

        var failureResult = Result.Failure<int>("Invalid quantity", ErrorCodes.Validation);

        Assert.False(failureResult.IsSuccess);
        Assert.True(failureResult.IsFailure);
        Assert.Throws<InvalidOperationException>(() => failureResult.Value);
    }

    [Fact]
    public void Generic_Result_Success_Throws_On_Null_Value()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }

    [Fact]
    public void Helper_Methods_Set_Expected_ErrorCodes()
    {
        var notFound = Result.NotFound("Ticket not found");
        Assert.Equal(ErrorCodes.NotFound, notFound.ErrorCode);

        var conflict = Result.Conflict("Ticket already reserved");
        Assert.Equal(ErrorCodes.Conflict, conflict.ErrorCode);

        var validation = Result.Validation("Invalid email");
        Assert.Equal(ErrorCodes.Validation, validation.ErrorCode);

        var unauthorized = Result.Unauthorized("Token expired");
        Assert.Equal(ErrorCodes.Unauthorized, unauthorized.ErrorCode);

        var forbidden = Result.Forbidden("Staff role required");
        Assert.Equal(ErrorCodes.Forbidden, forbidden.ErrorCode);
    }

    [Fact]
    public void Match_Executes_Correct_Branch()
    {
        var success = Result.Success("Hello");
        var successExecuted = success.Match(
            onSuccess: val => $"Success: {val}",
            onFailure: (err, _) => $"Error: {err}");
        Assert.Equal("Success: Hello", successExecuted);

        var failure = Result.Failure<string>("Failed to fetch", ErrorCodes.BadRequest);
        var failureExecuted = failure.Match(
            onSuccess: val => $"Success: {val}",
            onFailure: (err, code) => $"Error: {err} ({code})");
        Assert.Equal($"Error: Failed to fetch ({ErrorCodes.BadRequest})", failureExecuted);
    }

    [Fact]
    public void Equality_Compares_Values_Correctly()
    {
        var res1 = Result.Success(100);
        var res2 = Result.Success(100);
        var res3 = Result.Success(200);

        Assert.Equal(res1, res2);
        Assert.NotEqual(res1, res3);
        Assert.True(res1 == res2);
        Assert.False(res1 == res3);

        var fail1 = Result.Failure<int>("Error", ErrorCodes.Conflict);
        var fail2 = Result.Failure<int>("Error", ErrorCodes.Conflict);
        var fail3 = Result.Failure<int>("Different error", ErrorCodes.Conflict);

        Assert.Equal(fail1, fail2);
        Assert.NotEqual(fail1, fail3);
    }
}
