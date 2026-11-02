using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays;

/// <summary>
/// Loads holidays for an arbitrary date range by fetching the whole calendar year for every
/// year the range touches. Year-sized fetches are what the upstream API and the cache are
/// keyed on, so a range query for three days still reuses a cached year.
/// </summary>
public sealed class HolidayRangeLoader(IHolidayProvider provider)
{
    public async Task<Result<IReadOnlyList<Holiday>>> LoadAsync(
        CountryCode countryCode,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            return Result.Failure<IReadOnlyList<Holiday>>(HolidayErrors.RangeInverted);
        }

        if (!SupportedYears.Contains(from.Year) || !SupportedYears.Contains(to.Year))
        {
            return Result.Failure<IReadOnlyList<Holiday>>(
                HolidayErrors.YearOutOfRange(
                    SupportedYears.Contains(from.Year) ? to.Year : from.Year,
                    SupportedYears.Min,
                    SupportedYears.Max));
        }

        var collected = new List<Holiday>();

        for (int year = from.Year; year <= to.Year; year++)
        {
            Result<IReadOnlyList<Holiday>> yearResult =
                await provider.GetHolidaysAsync(countryCode, year, cancellationToken)
                    .ConfigureAwait(false);

            if (yearResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<Holiday>>(yearResult.Error);
            }

            collected.AddRange(yearResult.Value);
        }

        IReadOnlyList<Holiday> inRange =
        [
            .. collected
                .Where(holiday => holiday.Date >= from && holiday.Date <= to)
                .OrderBy(static holiday => holiday.Date)
                .ThenBy(static holiday => holiday.Name, StringComparer.Ordinal)
        ];

        return Result.Success(inRange);
    }
}
