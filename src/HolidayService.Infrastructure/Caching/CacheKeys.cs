using System.Globalization;
using HolidayService.Domain.Countries;

namespace HolidayService.Infrastructure.Caching;

internal static class CacheKeys
{
    public static string Holidays(CountryCode countryCode, int year) =>
        string.Create(CultureInfo.InvariantCulture, $"holidays:{countryCode.Value}:{year}");

    public const string WorldwideUpcoming = "holidays:worldwide:upcoming";
}
