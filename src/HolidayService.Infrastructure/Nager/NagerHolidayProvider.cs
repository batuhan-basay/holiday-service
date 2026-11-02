using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using Microsoft.Extensions.Logging;
using Polly.Timeout;

namespace HolidayService.Infrastructure.Nager;

/// <summary>
/// Talks to the public Nager.Date API. Transport faults are translated into domain errors so
/// the application layer never has to catch an <see cref="HttpRequestException"/>.
/// </summary>
internal sealed class NagerHolidayProvider(
    HttpClient httpClient,
    ILogger<NagerHolidayProvider> logger) : IHolidayProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<IReadOnlyList<Holiday>>> GetHolidaysAsync(
        CountryCode countryCode,
        int year,
        CancellationToken cancellationToken = default)
    {
        string path = string.Create(
            CultureInfo.InvariantCulture,
            $"api/v3/PublicHolidays/{year}/{countryCode.Value}");

        Result<List<NagerPublicHoliday>> response = await GetAsync<List<NagerPublicHoliday>>(
            path,
            CountryErrors.NotSupported(countryCode.Value),
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure)
        {
            return Result.Failure<IReadOnlyList<Holiday>>(response.Error);
        }

        IReadOnlyList<Holiday> holidays =
        [
            .. response.Value
                .Select(dto => dto.ToDomain(countryCode))
                .Where(static holiday => holiday is not null)
                .Select(static holiday => holiday!)
                .OrderBy(static holiday => holiday.Date)
        ];

        return Result.Success(holidays);
    }

    public async Task<Result<IReadOnlyList<CountrySummary>>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        Result<List<NagerCountry>> response = await GetAsync<List<NagerCountry>>(
            "api/v3/AvailableCountries",
            HolidayErrors.ProviderUnavailable,
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CountrySummary>>(response.Error);
        }

        IReadOnlyList<CountrySummary> countries =
        [
            .. response.Value
                .Select(static dto => dto.ToDomain())
                .Where(static country => country is not null)
                .Select(static country => country!)
        ];

        return Result.Success(countries);
    }

    public async Task<Result<Country>> GetCountryAsync(
        CountryCode countryCode,
        CancellationToken cancellationToken = default)
    {
        Result<NagerCountryInfo> response = await GetAsync<NagerCountryInfo>(
            $"api/v3/CountryInfo/{countryCode.Value}",
            CountryErrors.NotSupported(countryCode.Value),
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure)
        {
            return Result.Failure<Country>(response.Error);
        }

        Country? country = response.Value.ToDomain();

        return country is null
            ? Result.Failure<Country>(CountryErrors.NotSupported(countryCode.Value))
            : Result.Success(country);
    }

    public async Task<Result<IReadOnlyList<Holiday>>> GetNextHolidaysWorldwideAsync(
        CancellationToken cancellationToken = default)
    {
        Result<List<NagerPublicHoliday>> response = await GetAsync<List<NagerPublicHoliday>>(
            "api/v3/NextPublicHolidaysWorldwide",
            HolidayErrors.ProviderUnavailable,
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure)
        {
            return Result.Failure<IReadOnlyList<Holiday>>(response.Error);
        }

        CountryCode unknown = CountryCode.Create("ZZ").Value;

        IReadOnlyList<Holiday> holidays =
        [
            .. response.Value
                .Select(dto => dto.ToDomain(unknown))
                .Where(static holiday => holiday is not null)
                .Select(static holiday => holiday!)
        ];

        return Result.Success(holidays);
    }

    /// <summary>
    /// Single transport funnel: one place that decides how an upstream status code or
    /// transport fault becomes a domain error.
    /// </summary>
    private async Task<Result<TPayload>> GetAsync<TPayload>(
        string path,
        Error notFoundError,
        CancellationToken cancellationToken)
        where TPayload : class
    {
        try
        {
            using HttpResponseMessage response = await httpClient
                .GetAsync(new Uri(path, UriKind.Relative), cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                logger.LogInformation(
                    "Upstream returned {StatusCode} for {Path}; treating as not found",
                    (int)response.StatusCode,
                    path);

                return Result.Failure<TPayload>(notFoundError);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Upstream returned {StatusCode} for {Path}",
                    (int)response.StatusCode,
                    path);

                return Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable);
            }

            TPayload? payload = await response.Content
                .ReadFromJsonAsync<TPayload>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return payload is null
                ? Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable)
                : Result.Success(payload);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Transport failure while calling {Path}", path);
            return Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable);
        }
        catch (TimeoutRejectedException exception)
        {
            logger.LogError(exception, "Resilience pipeline timed out calling {Path}", path);
            return Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Unreadable payload from {Path}", path);
            return Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Request to {Path} timed out", path);
            return Result.Failure<TPayload>(HolidayErrors.ProviderUnavailable);
        }
    }
}
