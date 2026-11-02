using HolidayService.Api.ErrorHandling;
using HolidayService.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status503ServiceUnavailable, Type = typeof(ProblemDetails))]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    protected async Task<ActionResult<TResponse>> SendAsync<TResponse>(
        IRequest<Result<TResponse>> query,
        CancellationToken cancellationToken)
    {
        Result<TResponse> result = await Sender.Send(query, cancellationToken).ConfigureAwait(false);

        return result.ToActionResult(this);
    }
}
