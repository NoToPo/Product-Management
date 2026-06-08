using FluentValidation;
using MediatR;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Behaviors;

/// <summary>
/// MediatR pipeline step that runs all FluentValidation validators for a request
/// before it reaches the handler. On failure it short-circuits and returns a failed
/// <see cref="Result"/>/<see cref="Result{T}"/> (no exception) so the API can render
/// a 400 with per-field details.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToList();

        return CreateValidationResult<TResponse>(errors);
    }

    // Build the correct failed Result shape (Result or Result<T>) via reflection-free helpers.
    private static TResult CreateValidationResult<TResult>(IReadOnlyList<Error> errors)
        where TResult : Result
    {
        if (typeof(TResult) == typeof(Result))
            return (TResult)Result.Failure(errors);

        var valueType = typeof(TResult).GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(IReadOnlyList<Error>)])!
            .MakeGenericMethod(valueType);

        return (TResult)failureMethod.Invoke(null, [errors])!;
    }
}
