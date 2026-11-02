using HolidayService.Domain.Common;

namespace HolidayService.Domain.Holidays;

public static class HolidayErrors
{
    public static Error YearOutOfRange(int year, int min, int max) => Error.Validation(
        "Holiday.YearOutOfRange",
        $"Year {year} is outside the supported range {min}-{max}.");

    public static Error MonthOutOfRange(int month) => Error.Validation(
        "Holiday.MonthOutOfRange",
        $"Month {month} is outside the range 1-12.");

    public static readonly Error RangeInverted = Error.Validation(
        "Holiday.RangeInverted",
        "'from' must be earlier than or equal to 'to'.");

    public static Error RangeTooWide(int days, int maxDays) => Error.Validation(
        "Holiday.RangeTooWide",
        $"The requested range spans {days} days; at most {maxDays} are allowed per request.");

    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "Holiday.ProviderUnavailable",
        "The upstream holiday provider is currently unavailable. Please retry shortly.");

    public static Error NoneFound(string countryCode, int year) => Error.NotFound(
        "Holiday.NoneFound",
        $"No holidays are published for '{countryCode}' in {year}.");
}
