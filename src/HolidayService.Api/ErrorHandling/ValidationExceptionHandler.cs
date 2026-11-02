using HolidayService.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.Api.ErrorHandling;

internal sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (exception is not ValidationAppException validationException)
        {
            return false;
        }

        logger.LogInformation(
            "Rejected {Path} with {ErrorCount} validation error(s)",
            httpContext.Request.Path,
            validationException.Errors.Count);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetails = new ValidationProblemDetails(
            validationException.Errors.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request",
            Detail = "One or more query parameters failed validation.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = "Request.ValidationFailed";

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        }).ConfigureAwait(false);
    }
}
