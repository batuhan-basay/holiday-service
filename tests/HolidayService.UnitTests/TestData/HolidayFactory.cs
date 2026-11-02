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
        bool global = true,
        IReadOnlyList<string>? counties = null,
        HolidayType type = HolidayType.Public) =>
        new()
        {
            Date = new DateOnly(year, month, day),
            Name = name,
            LocalName = name,
            CountryCode = countryCode ?? Turkey,
            IsFixed = true,
            IsGlobal = global,
            Counties = counties ?? [],
            Types = [type]
        };
}
