using HolidayService.Application.Abstractions.Caching;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using HolidayService.Infrastructure.Calendars;
using Microsoft.Extensions.Options;

namespace HolidayService.Infrastructure.Caching;

internal sealed class CachedHolidayProvider(
    IHolidayProvider inner,
    ICacheService cache,
    IOptions<HolidayDataOptions> options) : IHolidayProvider
{
    private readonly HolidayDataOptions _options = options.Value;

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
                .SetAsync(key, result.Value, _options.CacheDuration, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    public Task<Result<IReadOnlyList<CountrySummary>>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default) =>
        inner.GetAvailableCountriesAsync(cancellationToken);

    public Task<Result<Country>> GetCountryAsync(
        CountryCode countryCode,
        CancellationToken cancellationToken = default) =>
        inner.GetCountryAsync(countryCode, cancellationToken);

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
}
