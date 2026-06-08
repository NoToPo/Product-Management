using ProductManagement.Domain.Common;

namespace ProductManagement.Api.Common;

/// <summary>
/// Translates a domain <see cref="Result"/> into an HTTP response. Failures become
/// RFC 7807 ProblemDetails with the status implied by the <see cref="ErrorType"/>.
/// </summary>
public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value) : ToProblem(result);

    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : ToProblem(result);

    private static IResult ToProblem(Result result)
    {
        var error = result.Error;

        if (error.Type == ErrorType.Validation)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray());

            return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest);
        }

        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            detail: error.Message,
            statusCode: status,
            title: error.Code);
    }
}
