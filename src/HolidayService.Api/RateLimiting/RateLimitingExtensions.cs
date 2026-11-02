using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HolidayService.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddClientRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RateLimitOptions settings = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(
                RateLimitOptions.PerClientPolicy,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = settings.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = $"The request quota of {settings.PermitLimit} per " +
                             $"{settings.WindowSeconds}s has been exhausted.",
                    Instance = context.HttpContext.Request.Path
                };

                problem.Extensions["errorCode"] = "Request.RateLimited";

                await context.HttpContext.Response
                    .WriteAsJsonAsync(problem, cancellationToken)
                    .ConfigureAwait(false);
            };
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            string? value = apiKey.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return $"key:{value.GetHashCode(StringComparison.Ordinal)}";
            }
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
