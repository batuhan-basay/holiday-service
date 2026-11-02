using Asp.Versioning;
using HolidayService.Api.RateLimiting;
using HolidayService.Application.Contracts;
using HolidayService.Application.Countries.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HolidayService.Api.Controllers.V1;

/// <summary>Countries the holiday data covers.</summary>
[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/countries")]
public sealed class CountriesController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>Lists every country the service can return holidays for.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The supported countries, ordered by name.</response>
    [HttpGet(Name = nameof(GetSupportedCountries))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<CountryResponse>))]
    public Task<ActionResult<IReadOnlyList<CountryResponse>>> GetSupportedCountries(
        CancellationToken cancellationToken) =>
        SendAsync(new GetCountries.Query(), cancellationToken);

    /// <summary>Returns one country's profile, including its land borders.</summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 code, e.g. <c>TR</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The country profile.</response>
    /// <response code="404">The country is not covered by the holiday data.</response>
    [HttpGet("{countryCode}", Name = nameof(GetCountry))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountryDetailResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<CountryDetailResponse>> GetCountry(
        string countryCode,
        CancellationToken cancellationToken) =>
        SendAsync(new GetCountryByCode.Query(countryCode), cancellationToken);
}
