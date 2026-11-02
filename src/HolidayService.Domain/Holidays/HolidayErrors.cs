using HolidayService.Domain.Common;

namespace HolidayService.Domain.Holidays;

public static class HolidayErrors
{
    public static Error YearOutOfRange(int year, int min, int max) => Error.Validation(
        "Holiday.YearOutOfRange",
        $"Year {year} is outside the supported range {min}-{max}.");

    public static readonly Error RangeInverted = Error.Validation(
        "Holiday.RangeInverted",
        "'from' must be earlier than or equal to 'to'.");

    public static Error RangeTooWide(int days, int maxDays) => Error.Validation(
        "Holiday.RangeTooWide",
        $"The requested range spans {days} days; at most {maxDays} are allowed per request.");
}
