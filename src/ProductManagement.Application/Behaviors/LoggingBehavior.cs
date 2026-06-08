using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using ProductManagement.Domain.Common;

namespace ProductManagement.Application.Behaviors;

/// <summary>Logs each request, its outcome (success/failure) and elapsed time.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next(cancellationToken);

        stopwatch.Stop();
        if (response.IsSuccess)
            logger.LogInformation("Handled {RequestName} in {Elapsed} ms", requestName, stopwatch.ElapsedMilliseconds);
        else
            logger.LogWarning("{RequestName} failed in {Elapsed} ms: {ErrorCode} {ErrorMessage}",
                requestName, stopwatch.ElapsedMilliseconds, response.Error.Code, response.Error.Message);

        return response;
    }
}
