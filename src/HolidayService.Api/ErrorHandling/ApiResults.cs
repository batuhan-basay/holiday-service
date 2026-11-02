using HolidayService.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.Api.ErrorHandling;

public static class ApiResults
{
    public static ActionResult<TValue> ToActionResult<TValue>(
        this Result<TValue> result,
        ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        return result.IsSuccess
            ? controller.Ok(result.Value)
            : Problem<TValue>(result.Error, controller);
    }

    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static ActionResult<TValue> Problem<TValue>(Error error, ControllerBase controller)
    {
        int statusCode = ToStatusCode(error.Type);

        ObjectResult problem = controller.Problem(
            detail: error.Description,
            statusCode: statusCode,
            title: TitleFor(error.Type));

        if (problem.Value is ProblemDetails details)
        {
            details.Extensions["errorCode"] = error.Code;
        }

        return problem;
    }

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Invalid request",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflicting state",
        ErrorType.Unavailable => "Service unavailable",
        _ => "Unexpected failure"
    };
}
