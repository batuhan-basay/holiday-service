using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.UnitTests.TestData;

internal static class HolidayFactory
{
    public static CountryCode Turkey { get; } = CountryCode.Create("TR").Value;

    public static CountryCode Germany { get; } = CountryCode.Create("DE").Value;

    public static Holiday On(
        int year,
        int month,
        int day,
        string name = "Test Holiday",
        CountryCode? countryCode = null,
        HolidayType type = HolidayType.Public,
        bool isSubstitute = false,
        bool isEstimated = false) =>
        new()
        {
            Date = new DateOnly(year, month, day),
            Name = name,
            LocalName = name,
            CountryCode = countryCode ?? Turkey,
            Types = [type],
            IsSubstitute = isSubstitute,
            IsEstimated = isEstimated
        };
}
