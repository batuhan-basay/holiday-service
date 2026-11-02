using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Domain.Calendar;

/// <summary>
/// Business-day arithmetic for one country. Holidays are indexed once on construction so
/// repeated day-by-day walks stay O(1) per day.
/// </summary>
public sealed class WorkCalendar
{
    private readonly Dictionary<DateOnly, Holiday> _holidaysByDate;
    private readonly WeekendDefinition _weekend;

    public WorkCalendar(
        CountryCode countryCode,
        IEnumerable<Holiday> holidays,
        WeekendDefinition? weekend = null)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        CountryCode = countryCode;
        _weekend = weekend ?? WeekendDefinition.ForCountry(countryCode);
        _holidaysByDate = holidays
            .Where(static h => h.IsPublic)
            .GroupBy(static h => h.Date)
            .ToDictionary(static g => g.Key, static g => g.First());
    }

    public CountryCode CountryCode { get; }

    public WeekendDefinition Weekend => _weekend;

    public bool IsWeekend(DateOnly date) => _weekend.Includes(date);

    public bool IsPublicHoliday(DateOnly date) => _holidaysByDate.ContainsKey(date);

    public Holiday? HolidayOn(DateOnly date) =>
        _holidaysByDate.TryGetValue(date, out Holiday? holiday) ? holiday : null;

    public bool IsWorkday(DateOnly date) => !IsWeekend(date) && !IsPublicHoliday(date);

    /// <summary>Counts workdays in an inclusive range. Returns 0 when the range is inverted.</summary>
    public int CountWorkdays(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return 0;
        }

        int count = 0;

        for (DateOnly day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkday(day))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Moves <paramref name="workdays"/> business days away from <paramref name="start"/>.
    /// The start date itself is never counted; a count of 0 returns it unchanged.
    /// Negative counts walk backwards.
    /// </summary>
    public DateOnly AddWorkdays(DateOnly start, int workdays)
    {
        if (workdays == 0)
        {
            return start;
        }

        int step = workdays > 0 ? 1 : -1;
        int remaining = Math.Abs(workdays);
        DateOnly current = start;

        while (remaining > 0)
        {
            current = current.AddDays(step);

            if (IsWorkday(current))
            {
                remaining--;
            }
        }

        return current;
    }

    /// <summary>The next workday strictly after <paramref name="date"/>.</summary>
    public DateOnly NextWorkday(DateOnly date) => AddWorkdays(date, 1);

    /// <summary>The previous workday strictly before <paramref name="date"/>.</summary>
    public DateOnly PreviousWorkday(DateOnly date) => AddWorkdays(date, -1);

    /// <summary>
    /// Finds every stretch of consecutive non-working days inside <paramref name="year"/>.
    /// A stretch qualifies as a long weekend once it reaches three days, or when a single
    /// bridge day would join a holiday to the weekend.
    /// </summary>
    public IReadOnlyList<LongWeekend> FindLongWeekends(int year, bool includeBridgeDays = true)
    {
        var results = new List<LongWeekend>();
        var firstOfYear = new DateOnly(year, 1, 1);
        var lastOfYear = new DateOnly(year, 12, 31);

        DateOnly cursor = firstOfYear;

        while (cursor <= lastOfYear)
        {
            if (IsWorkday(cursor))
            {
                cursor = cursor.AddDays(1);
                continue;
            }

            DateOnly start = cursor;
            DateOnly end = cursor;

            while (end.AddDays(1) <= lastOfYear && !IsWorkday(end.AddDays(1)))
            {
                end = end.AddDays(1);
            }

            int length = end.DayNumber - start.DayNumber + 1;

            if (length >= 3)
            {
                results.Add(new LongWeekend(start, end, NeedsBridgeDay: false));
            }
            else if (includeBridgeDays && length == 2)
            {
                LongWeekend? bridged = TryBridge(start, end, firstOfYear, lastOfYear);

                if (bridged is not null)
                {
                    results.Add(bridged);
                }
            }

            cursor = end.AddDays(1);
        }

        return results;
    }

    /// <summary>
    /// Checks whether a single day off immediately before or after a two-day stretch would
    /// extend it to at least four days.
    /// </summary>
    private LongWeekend? TryBridge(DateOnly start, DateOnly end, DateOnly yearStart, DateOnly yearEnd)
    {
        DateOnly dayBefore = start.AddDays(-1);
        DateOnly twoBefore = start.AddDays(-2);

        if (dayBefore >= yearStart && twoBefore >= yearStart && IsWorkday(dayBefore) && !IsWorkday(twoBefore))
        {
            return new LongWeekend(twoBefore, end, NeedsBridgeDay: true);
        }

        DateOnly dayAfter = end.AddDays(1);
        DateOnly twoAfter = end.AddDays(2);

        if (dayAfter <= yearEnd && twoAfter <= yearEnd && IsWorkday(dayAfter) && !IsWorkday(twoAfter))
        {
            return new LongWeekend(start, twoAfter, NeedsBridgeDay: true);
        }

        return null;
    }
}
