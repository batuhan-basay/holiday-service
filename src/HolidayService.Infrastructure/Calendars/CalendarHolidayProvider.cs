using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Infrastructure.Calendars;

internal sealed class CalendarHolidayProvider(
    EmbeddedCalendarCatalog catalog,
    IDateTimeProvider clock) : IHolidayProvider
{
    private const int WorldwideLookAheadDays = 60;

    public Task<Result<IReadOnlyList<Holiday>>> GetHolidaysAsync(
        CountryCode countryCode,
        int year,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!catalog.TryGet(countryCode, out HolidayCalendar calendar))
        {
            return Task.FromResult(
                Result.Failure<IReadOnlyList<Holiday>>(
                    CountryErrors.NotSupported(countryCode.Value)));
        }

        return Task.FromResult(Result.Success(calendar.ForYear(year)));
    }

    public Task<Result<IReadOnlyList<CountrySummary>>> GetAvailableCountriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Result.Success(catalog.Countries));
    }

    public Task<Result<Country>> GetCountryAsync(
        CountryCode countryCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            catalog.TryGet(countryCode, out HolidayCalendar calendar)
                ? Result.Success(calendar.ToCountry())
                : Result.Failure<Country>(CountryErrors.NotSupported(countryCode.Value)));
    }

    public Task<Result<IReadOnlyList<Holiday>>> GetNextHolidaysWorldwideAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateOnly today = clock.Today;
        DateOnly horizon = today.AddDays(WorldwideLookAheadDays);
        var collected = new List<Holiday>();

        foreach (CountrySummary country in catalog.Countries)
        {
            if (!catalog.TryGet(country.Code, out HolidayCalendar calendar))
            {
                continue;
            }

            collected.AddRange(calendar.ForYear(today.Year));

            if (horizon.Year != today.Year)
            {
                collected.AddRange(calendar.ForYear(horizon.Year));
            }
        }

        IReadOnlyList<Holiday> upcoming =
        [
            .. collected
                .Where(holiday => holiday.Date >= today && holiday.Date <= horizon)
                .OrderBy(static holiday => holiday.Date)
                .ThenBy(static holiday => holiday.CountryCode.Value, StringComparer.Ordinal)
        ];

        return Task.FromResult(Result.Success(upcoming));
    }
}
