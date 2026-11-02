using System.Diagnostics;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Domain.Common;
using Microsoft.Extensions.Logging;

namespace HolidayService.Application.Behaviors;

internal sealed class LoggingBehavior<TQuery, TResponse>(
    ILogger<LoggingBehavior<TQuery, TResponse>> logger)
    : IQueryBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<Result<TResponse>> HandleAsync(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        string queryName = typeof(TQuery).Name;
        long startedAt = Stopwatch.GetTimestamp();

        Result<TResponse> response = await next().ConfigureAwait(false);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startedAt);

        if (response.IsFailure)
        {
            logger.LogWarning(
                "{QueryName} failed in {ElapsedMs}ms with {ErrorCode}: {ErrorDescription}",
                queryName,
                (long)elapsed.TotalMilliseconds,
                response.Error.Code,
                response.Error.Description);
        }
        else if (elapsed.TotalMilliseconds >= SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "{QueryName} completed in {ElapsedMs}ms, above the {ThresholdMs}ms threshold",
                queryName,
                (long)elapsed.TotalMilliseconds,
                SlowRequestThresholdMs);
        }
        else
        {
            logger.LogInformation(
                "{QueryName} completed in {ElapsedMs}ms",
                queryName,
                (long)elapsed.TotalMilliseconds);
        }

        return response;
    }
}
