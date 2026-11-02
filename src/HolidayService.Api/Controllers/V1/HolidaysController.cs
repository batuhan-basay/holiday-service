using Asp.Versioning;
using HolidayService.Api.RateLimiting;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Domain.Holidays;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HolidayService.Api.Controllers.V1;

/// <summary>Public holiday lookups.</summary>
[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/holidays")]
public sealed class HolidaysController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>All holidays published for a country in a calendar year.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code, e.g. <c>TR</c>.</param>
    /// <param name="year">Four-digit year.</param>
    /// <param name="county">Optional ISO 3166-2 subdivision filter, e.g. <c>DE-BY</c>.</param>
    /// <param name="type">Optional holiday type filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The holidays for that year.</response>
    /// <response code="404">The country is not covered by the holiday data.</response>
    [HttpGet("{countryCode}/{year:int}", Name = nameof(GetByYear))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByYear(
        string countryCode,
        int year,
        CancellationToken cancellationToken,
        [FromQuery] string? county = null,
        [FromQuery] HolidayType? type = null) =>
        SendAsync(new GetHolidaysByYear.Query(countryCode, year, county, type), cancellationToken);

    /// <summary>Holidays falling inside a single month.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="year">Four-digit year.</param>
    /// <param name="month">Month number, 1-12.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The holidays in that month.</response>
    [HttpGet("{countryCode}/{year:int}/{month:int}", Name = nameof(GetByMonth))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByMonth(
        string countryCode,
        int year,
        int month,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidaysByMonth.Query(countryCode, year, month), cancellationToken);

    /// <summary>Holidays inside an arbitrary inclusive date range.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="from">First date of the range (inclusive), <c>yyyy-MM-dd</c>.</param>
    /// <param name="to">Last date of the range (inclusive), <c>yyyy-MM-dd</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The holidays in that range.</response>
    [HttpGet("{countryCode}/range", Name = nameof(GetByRange))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetByRange(
        string countryCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidaysByRange.Query(countryCode, from, to), cancellationToken);

    /// <summary>The next holidays from today, crossing into next year when needed.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="count">How many holidays to return, 1-50.</param>
    /// <response code="200">The next holidays.</response>
    [HttpGet("{countryCode}/next", Name = nameof(GetNext))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetNext(
        string countryCode,
        CancellationToken cancellationToken,
        [FromQuery] int count = 5) =>
        SendAsync(new GetNextHolidays.Query(countryCode, count), cancellationToken);

    /// <summary>Holidays falling within the next N days.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="days">Size of the look-ahead window in days, 1-365.</param>
    /// <response code="200">The upcoming holidays.</response>
    [HttpGet("{countryCode}/upcoming", Name = nameof(GetUpcoming))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayCollectionResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayCollectionResponse>> GetUpcoming(
        string countryCode,
        CancellationToken cancellationToken,
        [FromQuery] int days = 30) =>
        SendAsync(new GetUpcomingHolidays.Query(countryCode, days), cancellationToken);

    /// <summary>Stretches of consecutive days off in a year.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="year">Four-digit year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="includeBridgeDays">
    /// When true, also returns stretches that need one bridge day to materialise.
    /// </param>
    /// <response code="200">The long weekends of that year.</response>
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

    /// <summary>Aggregated holiday statistics for a country and year.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="year">Four-digit year.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Counts by month and weekday, plus weekend overlap.</response>
    [HttpGet("{countryCode}/statistics/{year:int}", Name = nameof(GetStatistics))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HolidayStatisticsResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<HolidayStatisticsResponse>> GetStatistics(
        string countryCode,
        int year,
        CancellationToken cancellationToken) =>
        SendAsync(new GetHolidayStatistics.Query(countryCode, year), cancellationToken);

    /// <summary>Checks whether one date is a public holiday, a weekend day or a workday.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code.</param>
    /// <param name="date">The date to check, <c>yyyy-MM-dd</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The status of that date.</response>
    [HttpGet("{countryCode}/check", Name = nameof(CheckDate))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DayStatusResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<DayStatusResponse>> CheckDate(
        string countryCode,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        SendAsync(new GetDayStatus.Query(countryCode, date), cancellationToken);

    /// <summary>The next upcoming holidays across every supported country.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Upcoming holidays worldwide.</response>
    [HttpGet("worldwide/upcoming", Name = nameof(GetWorldwideUpcoming))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<HolidayResponse>))]
    public Task<ActionResult<IReadOnlyList<HolidayResponse>>> GetWorldwideUpcoming(
        CancellationToken cancellationToken) =>
        SendAsync(new GetWorldwideUpcomingHolidays.Query(), cancellationToken);
}
