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

[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workdays")]
public sealed class WorkdaysController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet("{countryCode}/count", Name = nameof(Count))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkdayCountResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<WorkdayCountResponse>> Count(
        string countryCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        SendAsync(new CountWorkdays.Query(countryCode, from, to), cancellationToken);

    [HttpGet("{countryCode}/shift", Name = nameof(Shift))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkdayShiftResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<WorkdayShiftResponse>> Shift(
        string countryCode,
        [FromQuery] DateOnly startDate,
        [FromQuery] int workdays,
        CancellationToken cancellationToken) =>
        SendAsync(new ShiftWorkdays.Query(countryCode, startDate, workdays), cancellationToken);

    [HttpGet("{countryCode}/status", Name = nameof(Status))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DayStatusResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<DayStatusResponse>> Status(
        string countryCode,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        SendAsync(new GetDayStatus.Query(countryCode, date), cancellationToken);
}
