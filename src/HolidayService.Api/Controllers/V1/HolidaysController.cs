using Asp.Versioning;
using HolidayService.Api.RateLimiting;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Domain.Holidays;
using HolidayService.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HolidayService.Api.Controllers.V1;

[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
public sealed class HolidaysController(IQueryDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet("{countryCode}/{year:int}", Name = nameof(GetByYear))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByYear(
        string countryCode,
        int year,
        CancellationToken cancellationToken,
        [FromQuery] HolidayType? type = null) =>
        SendAsync(new GetHolidaysByYear.Query(countryCode, year, type), cancellationToken);

    [HttpGet("{countryCode}/{year:int}/{month:int}", Name = nameof(GetByMonth))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByMonth(
        string countryCode,
        int year,
        int month,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidaysByMonth.Query(countryCode, year, month), cancellationToken);

    [HttpGet("{countryCode}/range", Name = nameof(GetByRange))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByRange(
        string countryCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidaysByRange.Query(countryCode, from, to), cancellationToken);

    [HttpGet("{countryCode}/next", Name = nameof(GetNext))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetNext(
        string countryCode,
        CancellationToken cancellationToken,
        [FromQuery] int count = 5) =>
        SendAsync(new GetNextHolidays.Query(countryCode, count), cancellationToken);

    [HttpGet("{countryCode}/upcoming", Name = nameof(GetUpcoming))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetUpcoming(
        string countryCode,
        CancellationToken cancellationToken,
        [FromQuery] int days = 30) =>
        SendAsync(new GetUpcomingHolidays.Query(countryCode, days), cancellationToken);

    [HttpGet("{countryCode}/long-weekends/{year:int}", Name = nameof(GetLongWeekendsForYear))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<LongWeekendResponse>))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<IReadOnlyList<LongWeekendResponse>>> GetLongWeekendsForYear(
        string countryCode,
        int year,
        CancellationToken cancellationToken,
        [FromQuery] bool includeBridgeDays = true) =>
        SendAsync(
            new GetLongWeekends.Query(countryCode, year, includeBridgeDays),
            cancellationToken);

    [HttpGet("{countryCode}/statistics/{year:int}", Name = nameof(GetStatistics))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayStatisticsResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayStatisticsResponse>> GetStatistics(
        string countryCode,
        int year,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidayStatistics.Query(countryCode, year), cancellationToken);

    [HttpGet("{countryCode}/check", Name = nameof(CheckDate))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DayStatusResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<DayStatusResponse>> CheckDate(
        string countryCode,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        SendAsync(new GetDayStatus.Query(countryCode, date), cancellationToken);

    [HttpGet("worldwide/upcoming", Name = nameof(GetWorldwideUpcoming))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<HolidayResponse>))]
    public Task<ActionResult<IReadOnlyList<HolidayResponse>>> GetWorldwideUpcoming(
        CancellationToken cancellationToken) =>
        SendAsync(new GetWorldwideUpcomingHolidays.Query(), cancellationToken);
}
