using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays;

public static class HolidayFilter
{
    public static IEnumerable<Holiday> Apply(
        this IEnumerable<Holiday> holidays,
        string? county,
        HolidayType? type)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        IEnumerable<Holiday> filtered = holidays;

        if (!string.IsNullOrWhiteSpace(county))
        {
            string trimmed = county.Trim();
            filtered = filtered.Where(holiday => holiday.AppliesTo(trimmed));
        }

        if (type is not null)
        {
            filtered = filtered.Where(holiday => holiday.Types.Contains(type.Value));
        }

        return filtered;
    }
}
