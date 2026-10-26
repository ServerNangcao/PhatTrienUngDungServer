using System.Diagnostics.CodeAnalysis;

namespace Ticketing.SharedKernel;

/// <summary>
/// Result Pattern: Mô hình hóa kết quả của một tác vụ thay thế việc ném exception cho luồng dự kiến.
/// Đảm bảo tính bất biến (immutability) và bảo toàn tính toàn vẹn (invariant validation).
/// </summary>
public class Result : IEquatable<Result>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        if (isSuccess && (error is not null || errorCode is not null))
        {
            throw new InvalidOperationException("A success result cannot specify an error or error code.");
        }

        if (!isSuccess && string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("A failure result must specify a non-empty error message.");
        }

        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    // ───────────────────────────────────────────────
    // Factory methods cho Result phi generic
    // ───────────────────────────────────────────────
    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);

    public static Result NotFound(string error) => Failure(error, ErrorCodes.NotFound);
    public static Result Conflict(string error) => Failure(error, ErrorCodes.Conflict);
    public static Result Validation(string error) => Failure(error, ErrorCodes.Validation);
    public static Result Unauthorized(string error) => Failure(error, ErrorCodes.Unauthorized);
    public static Result Forbidden(string error) => Failure(error, ErrorCodes.Forbidden);

    // ───────────────────────────────────────────────
    // Factory methods cho Result<T> trên lớp phi generic (Tuân thủ CA1000 & Type Inference)
    // ───────────────────────────────────────────────
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => Result<T>.Failure(error, errorCode);

    public static Result<T> NotFound<T>(string error) => Failure<T>(error, ErrorCodes.NotFound);
    public static Result<T> Conflict<T>(string error) => Failure<T>(error, ErrorCodes.Conflict);
    public static Result<T> Validation<T>(string error) => Failure<T>(error, ErrorCodes.Validation);
    public static Result<T> Unauthorized<T>(string error) => Failure<T>(error, ErrorCodes.Unauthorized);
    public static Result<T> Forbidden<T>(string error) => Failure<T>(error, ErrorCodes.Forbidden);

    // ───────────────────────────────────────────────
    // Pattern Matching
    // ───────────────────────────────────────────────
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<string, string?, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess() : onFailure(Error!, ErrorCode);
    }

    // ───────────────────────────────────────────────
    // Equality
    // ───────────────────────────────────────────────
    public bool Equals(Result? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (IsSuccess != other.IsSuccess) return false;
        if (IsSuccess) return true;

        return string.Equals(Error, other.Error, StringComparison.Ordinal)
            && string.Equals(ErrorCode, other.ErrorCode, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as Result);

    public override int GetHashCode() =>
        IsSuccess
            ? HashCode.Combine(true)
            : HashCode.Combine(false, Error, ErrorCode);

    public static bool operator ==(Result? left, Result? right) => Equals(left, right);
    public static bool operator !=(Result? left, Result? right) => !Equals(left, right);
}

/// <summary>
/// Result có kèm dữ liệu trả về kiểu T khi thành công.
/// Ngăn chặn NullReferenceException ngầm bằng cách chặn truy cập .Value khi IsFailure.
/// </summary>
public sealed class Result<T> : Result, IEquatable<Result<T>>
{
    private readonly T? _value;

    /// <summary>
    /// Giá trị trả về khi thành công.
    /// Ném InvalidOperationException nếu cố truy cập khi kết quả thất bại (IsFailure).
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed. Check IsSuccess or IsFailure first.");

    internal Result(T value) : base(true, null, null)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    internal Result(string error, string? errorCode) : base(false, error, errorCode)
    {
        _value = default;
    }

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Standard Result pattern convenience syntax")]
    public static Result<T> Success(T value) => new(value);

    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Standard Result pattern convenience syntax")]
    public new static Result<T> Failure(string error, string? errorCode = null) => new(error, errorCode);

    /// <summary>
    /// Cho phép gán giá trị trực tiếp: Result&lt;int&gt; r = 42;
    /// </summary>
    public static implicit operator Result<T>(T value) => new(value);

    // ───────────────────────────────────────────────
    // Pattern Matching
    // ───────────────────────────────────────────────
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, string?, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return IsSuccess ? onSuccess(_value!) : onFailure(Error!, ErrorCode);
    }

    // ───────────────────────────────────────────────
    // Equality
    // ───────────────────────────────────────────────
    public bool Equals(Result<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (IsSuccess != other.IsSuccess) return false;

        if (IsSuccess)
        {
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        return string.Equals(Error, other.Error, StringComparison.Ordinal)
            && string.Equals(ErrorCode, other.ErrorCode, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as Result<T>);

    public override int GetHashCode() =>
        IsSuccess
            ? HashCode.Combine(true, _value)
            : HashCode.Combine(false, Error, ErrorCode);

    public static bool operator ==(Result<T>? left, Result<T>? right) => Equals(left, right);
    public static bool operator !=(Result<T>? left, Result<T>? right) => !Equals(left, right);
}
