using System.Globalization;

namespace HolidayService.Domain.Calendar;

public static class HijriCalculator
{
    private static readonly HijriCalendar Calendar = new();

    public static IReadOnlyList<DateOnly> OccurrencesIn(
        int gregorianYear,
        int hijriMonth,
        int hijriDay,
        int dayAdjustment = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hijriMonth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hijriMonth, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(hijriDay, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(hijriDay, 30);

        int approximate = (int)((gregorianYear - 621.5709) / 0.970224);
        var found = new List<DateOnly>();

        for (int hijriYear = approximate - 1; hijriYear <= approximate + 1; hijriYear++)
        {
            if (!TryResolve(hijriYear, hijriMonth, hijriDay, dayAdjustment, out DateOnly date))
            {
                continue;
            }

            if (date.Year == gregorianYear && !found.Contains(date))
            {
                found.Add(date);
            }
        }

        found.Sort();

        return found;
    }

    private static bool TryResolve(
        int hijriYear,
        int hijriMonth,
        int hijriDay,
        int dayAdjustment,
        out DateOnly date)
    {
        date = default;

        try
        {
            if (hijriYear < Calendar.GetYear(Calendar.MinSupportedDateTime.AddDays(1))
                || hijriYear > Calendar.GetYear(Calendar.MaxSupportedDateTime.AddDays(-1)))
            {
                return false;
            }

            int lengthOfMonth = Calendar.GetDaysInMonth(hijriYear, hijriMonth);

            if (hijriDay > lengthOfMonth)
            {
                return false;
            }

            DateTime gregorian = Calendar
                .ToDateTime(hijriYear, hijriMonth, hijriDay, 0, 0, 0, 0)
                .AddDays(dayAdjustment);

            date = DateOnly.FromDateTime(gregorian);

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
