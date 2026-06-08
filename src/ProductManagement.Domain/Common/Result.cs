namespace ProductManagement.Domain.Common;

/// <summary>
/// Outcome of an operation. Avoids throwing exceptions for expected failures
/// (validation, not-found, conflict) so the flow stays explicit and testable.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
            throw new InvalidOperationException("A successful result cannot contain errors.");
        if (!isSuccess && errors.Count == 0)
            throw new InvalidOperationException("A failed result must contain at least one error.");

        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>The primary error (first), or <see cref="Error.None"/> on success.</summary>
    public Error Error => Errors.Count > 0 ? Errors[0] : Error.None;

    public static Result Success() => new(true, []);
    public static Result Failure(Error error) => new(false, [error]);
    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(IReadOnlyList<Error> errors) => Result<T>.Failure(errors);

    // Ergonomic implicit conversion: `return error;` from a method returning Result.
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, []) => _value = value;
    private Result(IReadOnlyList<Error> errors) : base(false, errors) => _value = default;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new([error]);
    public static new Result<T> Failure(IReadOnlyList<Error> errors) => new(errors);

    // Ergonomic implicit conversions: `return value;` or `return error;`
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
