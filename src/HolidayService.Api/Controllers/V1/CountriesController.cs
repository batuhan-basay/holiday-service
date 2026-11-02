using Asp.Versioning;
using HolidayService.Api.RateLimiting;
using HolidayService.Application.Contracts;
using HolidayService.Application.Countries.Queries;
using HolidayService.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HolidayService.Api.Controllers.V1;

[Authorize]
[EnableRateLimiting(RateLimitOptions.PerClientPolicy)]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/countries")]
public sealed class CountriesController(IQueryDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpGet(Name = nameof(GetSupportedCountries))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<CountryResponse>))]
    public Task<ActionResult<IReadOnlyList<CountryResponse>>> GetSupportedCountries(
        CancellationToken cancellationToken) =>
        SendAsync(new GetCountries.Query(), cancellationToken);

    [HttpGet("{countryCode}", Name = nameof(GetCountry))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CountryDetailResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<ActionResult<CountryDetailResponse>> GetCountry(
        string countryCode,
        CancellationToken cancellationToken) =>
        SendAsync(new GetCountryByCode.Query(countryCode), cancellationToken);
}
