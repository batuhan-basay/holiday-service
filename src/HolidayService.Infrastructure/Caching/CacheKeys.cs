using System.Globalization;
using HolidayService.Domain.Countries;

namespace HolidayService.Infrastructure.Caching;

/// <summary>One place that owns the cache key format, so keys cannot drift between callers.</summary>
internal static class CacheKeys
{
    public static string Holidays(CountryCode countryCode, int year) =>
        string.Create(CultureInfo.InvariantCulture, $"holidays:{countryCode.Value}:{year}");

    public const string AvailableCountries = "countries:available";

    public static string Country(CountryCode countryCode) => $"country:{countryCode.Value}";

    public const string WorldwideUpcoming = "holidays:worldwide:upcoming";
}
