using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Abstractions.Providers;

public interface IHolidayProvider
{
    Task<Result<IReadOnlyList<Holiday>>> GetHolidaysAsync(
        CountryCode countryCode,
        int year,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CountrySummary>>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Country>> GetCountryAsync(
        CountryCode countryCode,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Holiday>>> GetNextHolidaysWorldwideAsync(
        CancellationToken cancellationToken = default);
}
