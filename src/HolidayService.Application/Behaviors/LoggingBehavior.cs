using System.Diagnostics;
using HolidayService.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HolidayService.Application.Behaviors;

/// <summary>
/// Emits one structured log line per request with its outcome and duration. Failed results
/// are logged at warning level with their error code, which keeps expected failures out of
/// the exception pipeline while remaining visible in logs.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        long startedAt = Stopwatch.GetTimestamp();

        TResponse response = await next().ConfigureAwait(false);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (response.IsSuccess)
        {
            if (elapsed.TotalMilliseconds >= SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "{RequestName} completed in {ElapsedMs}ms, above the {ThresholdMs}ms threshold",
                    requestName,
                    (long)elapsed.TotalMilliseconds,
                    SlowRequestThresholdMs);
            }
            else
            {
                logger.LogInformation(
                    "{RequestName} completed in {ElapsedMs}ms",
                    requestName,
                    (long)elapsed.TotalMilliseconds);
            }
        }
        else
        {
            logger.LogWarning(
                "{RequestName} failed in {ElapsedMs}ms with {ErrorCode}: {ErrorDescription}",
                requestName,
                (long)elapsed.TotalMilliseconds,
                response.Error.Code,
                response.Error.Description);
        }

        return response;
    }
}
