using HolidayService.Api.ErrorHandling;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ProblemDetails))]
public abstract class ApiControllerBase(IQueryDispatcher dispatcher) : ControllerBase
{
    protected IQueryDispatcher Dispatcher { get; } = dispatcher;

    protected async Task<ActionResult<TResponse>> SendAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken)
    {
        Result<TResponse> result = await Dispatcher
            .SendAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return result.ToActionResult(this);
    }
}
