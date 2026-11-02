using Asp.Versioning;
using HolidayService.Api.RateLimiting;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Application.Workdays.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HolidayService.Api.Controllers.V1;

/// <summary>Business-day arithmetic derived from the holiday calendar.</summary>
[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workdays")]
public sealed class WorkdaysController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Breaks an inclusive range into workdays, weekend days and public holidays.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="from">First date of the range (inclusive).</param>
    /// <param name="to">Last date of the range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The day breakdown for that range.</response>
    [HttpGet("{countryCode}/count", Name = nameof(Count))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkdayCountResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<WorkdayCountResponse>> Count(
        string countryCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        SendAsync(new CountWorkdays.Query(countryCode, from, to), cancellationToken);

    /// <summary>
    /// Moves a date by a number of business days, skipping weekends and public holidays.
    /// Negative values walk backwards.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="startDate">The date to shift from; it is never counted itself.</param>
    /// <param name="workdays">Business days to move, -500 to 500.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The resulting date.</response>
    [HttpGet("{countryCode}/shift", Name = nameof(Shift))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkdayShiftResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<WorkdayShiftResponse>> Shift(
        string countryCode,
        [FromQuery] DateOnly startDate,
        [FromQuery] int workdays,
        CancellationToken cancellationToken) =>
        SendAsync(new ShiftWorkdays.Query(countryCode, startDate, workdays), cancellationToken);

    /// <summary>Checks whether a single date is a workday in the given country.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="date">The date to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The status of that date.</response>
    [HttpGet("{countryCode}/status", Name = nameof(Status))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DayStatusResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<DayStatusResponse>> Status(
        string countryCode,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        SendAsync(new GetDayStatus.Query(countryCode, date), cancellationToken);
}
