using HolidayService.Api.ErrorHandling;
using HolidayService.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.Api.Controllers;

/// <summary>
/// Shared base for every versioned controller: the JSON content type, the error responses
/// every endpoint can produce, and the Result-to-HTTP translation live here once.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(ProblemDetails))]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    /// <summary>Dispatches a query and translates its result into an HTTP response.</summary>
    protected async Task<ActionResult<TResponse>> SendAsync<TResponse>(
        IRequest<Result<TResponse>> query,
        CancellationToken cancellationToken)
    {
        Result<TResponse> result = await Sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.ToActionResult(this);
    }
}
