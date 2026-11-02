using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using HolidayService.Infrastructure.Nager;

namespace HolidayService.UnitTests.Mapping;

public sealed class NagerMapperTests
{
    private static readonly CountryCode Fallback = CountryCode.Create("TR").Value;

    [Fact]
    public void ToDomain_MapsAFullPayload()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 1, 1),
            Name = "New Year's Day",
            LocalName = "Yılbaşı",
            CountryCode = "tr",
            Fixed = true,
            Global = true,
            Counties = null,
            LaunchYear = 1935,
            Types = ["Public"]
        };

        Holiday? holiday = dto.ToDomain(Fallback);

        holiday.ShouldNotBeNull();
        holiday!.Name.ShouldBe("New Year's Day");
        holiday.LocalName.ShouldBe("Yılbaşı");
        holiday.CountryCode.Value.ShouldBe("TR");
        holiday.LaunchYear.ShouldBe(1935);
        holiday.Types.ShouldBe(new[] { HolidayType.Public });
        holiday.IsPublic.ShouldBeTrue();
    }

    [Fact]
    public void ToDomain_FallsBackToTheRequestedCountryWhenUpstreamOmitsIt()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 5, 1),
            Name = "Labour Day",
            CountryCode = null
        };

        Holiday? holiday = dto.ToDomain(Fallback);

        holiday!.CountryCode.ShouldBe(Fallback);
    }

    [Fact]
    public void ToDomain_UsesTheLocalNameWhenTheEnglishNameIsMissing()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 5, 1),
            Name = null,
            LocalName = "Emek ve Dayanışma Günü"
        };

        Holiday? holiday = dto.ToDomain(Fallback);

        holiday!.Name.ShouldBe("Emek ve Dayanışma Günü");
    }

    [Fact]
    public void ToDomain_DropsEntriesWithoutAnyName()
    {
        var dto = new NagerPublicHoliday { Date = new DateOnly(2026, 5, 1) };

        dto.ToDomain(Fallback).ShouldBeNull();
    }

    [Fact]
    public void ToDomain_DefaultsToPublicWhenNoTypeIsSupplied()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 5, 1),
            Name = "Labour Day",
            Types = []
        };

        dto.ToDomain(Fallback)!.Types.ShouldBe(new[] { HolidayType.Public });
    }

    [Fact]
    public void ToDomain_MapsUnknownTypesWithoutThrowing()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 5, 1),
            Name = "Something",
            Types = ["Public", "SomethingNew"]
        };

        dto.ToDomain(Fallback)!.Types
            .ShouldBe(new[] { HolidayType.Public, HolidayType.Unknown });
    }

    [Fact]
    public void ToDomain_KeepsRegionalScope()
    {
        var dto = new NagerPublicHoliday
        {
            Date = new DateOnly(2026, 11, 1),
            Name = "All Saints",
            CountryCode = "DE",
            Global = false,
            Counties = ["DE-BY", "DE-NW"]
        };

        Holiday holiday = dto.ToDomain(Fallback)!;

        holiday.IsGlobal.ShouldBeFalse();
        holiday.AppliesTo("DE-BY").ShouldBeTrue();
        holiday.AppliesTo("DE-HE").ShouldBeFalse();
    }

    [Fact]
    public void ToDomain_SkipsCountriesWithAnUnusableCode()
    {
        new NagerCountry { CountryCode = "XYZ", Name = "Nowhere" }.ToDomain().ShouldBeNull();
    }

    [Fact]
    public void ToDomain_MapsCountryInfoWithBorders()
    {
        var dto = new NagerCountryInfo
        {
            CommonName = "Türkiye",
            OfficialName = "Republic of Türkiye",
            CountryCode = "TR",
            Region = "Asia",
            Borders =
            [
                new NagerCountryInfo { CountryCode = "GR", CommonName = "Greece" },
                new NagerCountryInfo { CountryCode = "BAD", CommonName = "Broken" }
            ]
        };

        Country country = dto.ToDomain()!;

        country.Code.Value.ShouldBe("TR");
        country.Region.ShouldBe("Asia");
        country.Borders.ShouldHaveSingleItem().Code.Value.ShouldBe("GR");
    }
}
