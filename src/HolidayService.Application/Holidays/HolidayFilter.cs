using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays;

public static class HolidayFilter
{
    public static IEnumerable<Holiday> Apply(this IEnumerable<Holiday> holidays, HolidayType? type)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        return type is null
            ? holidays
            : holidays.Where(holiday => holiday.Types.Contains(type.Value));
    }
}
