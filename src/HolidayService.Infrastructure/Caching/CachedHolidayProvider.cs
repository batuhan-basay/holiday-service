using HolidayService.Application.Abstractions.Caching;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using HolidayService.Infrastructure.Nager;
using Microsoft.Extensions.Options;

namespace HolidayService.Infrastructure.Caching;

/// <summary>
/// Decorator that adds cache-aside behaviour to any <see cref="IHolidayProvider"/>.
/// <para>
/// Caching lives here rather than in a MediatR behavior on purpose: every query in the
/// application layer is composed from whole-year holiday sets, so caching at the provider
/// makes one upstream call serve the year, month, range, upcoming, long-weekend and
/// workday queries alike.
/// </para>
/// Only successful results are cached — a provider outage must not be remembered.
/// </summary>
internal sealed class CachedHolidayProvider(
    IHolidayProvider inner,
    ICacheService cache,
    IDateTimeProvider clock,
    IOptions<NagerDateOptions> options) : IHolidayProvider
{
    private readonly NagerDateOptions _options = options.Value;

    public async Task<Result<IReadOnlyList<Holiday>>> GetHolidaysAsync(
        CountryCode countryCode,
        int year,
        CancellationToken cancellationToken = default)
    {
        string key = CacheKeys.Holidays(countryCode, year);

        IReadOnlyList<Holiday>? cached = await cache
            .GetAsync<IReadOnlyList<Holiday>>(key, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        Result<IReadOnlyList<Holiday>> result = await inner
            .GetHolidaysAsync(countryCode, year, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await cache
                .SetAsync(key, result.Value, ExpirationFor(year), cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<CountrySummary>>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CountrySummary>? cached = await cache
            .GetAsync<IReadOnlyList<CountrySummary>>(CacheKeys.AvailableCountries, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        Result<IReadOnlyList<CountrySummary>> result = await inner
            .GetAvailableCountriesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await cache.SetAsync(
                CacheKeys.AvailableCountries,
                result.Value,
                _options.HistoricalCacheDuration,
                cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<Country>> GetCountryAsync(
        CountryCode countryCode,
        CancellationToken cancellationToken = default)
    {
        string key = CacheKeys.Country(countryCode);

        Country? cached = await cache
            .GetAsync<Country>(key, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        Result<Country> result = await inner
            .GetCountryAsync(countryCode, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await cache.SetAsync(
                key,
                result.Value,
                _options.HistoricalCacheDuration,
                cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Result<IReadOnlyList<Holiday>>> GetNextHolidaysWorldwideAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Holiday>? cached = await cache
            .GetAsync<IReadOnlyList<Holiday>>(CacheKeys.WorldwideUpcoming, cancellationToken)
            .ConfigureAwait(false);

        if (cached is not null)
        {
            return Result.Success(cached);
        }

        Result<IReadOnlyList<Holiday>> result = await inner
            .GetNextHolidaysWorldwideAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await cache.SetAsync(
                CacheKeys.WorldwideUpcoming,
                result.Value,
                _options.CacheDuration,
                cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Past years no longer change, so they are held far longer than the current year.
    /// </summary>
    private TimeSpan ExpirationFor(int year) => year < clock.Today.Year
        ? _options.HistoricalCacheDuration
        : _options.CacheDuration;
}
