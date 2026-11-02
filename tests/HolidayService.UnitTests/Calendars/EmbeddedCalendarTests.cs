using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using HolidayService.Infrastructure.Calendars;
using Microsoft.Extensions.Options;

namespace HolidayService.UnitTests.Calendars;

public sealed class EmbeddedCalendarTests
{
    private static readonly EmbeddedCalendarCatalog Catalog =
        new(Options.Create(new HolidayDataOptions()));

    private static IReadOnlyList<DateOnly> DatesFor(string code, int year)
    {
        Catalog.TryGet(CountryCode.Create(code).Value, out HolidayCalendar calendar)
            .ShouldBeTrue($"'{code}' should be an embedded calendar");

        return [.. calendar.ForYear(year).Select(static holiday => holiday.Date)];
    }

    [Fact]
    public void Catalog_ExposesTheFiveSupportedCountries() =>
        Catalog.Countries.Select(static country => country.Code.Value)
            .ShouldBe(new[] { "DE", "ES", "GB", "TR", "US" }, ignoreOrder: true);

    [Fact]
    public void Catalog_RejectsACountryItDoesNotCover() =>
        Catalog.TryGet(CountryCode.Create("FR").Value, out _).ShouldBeFalse();

    private static IReadOnlyList<Holiday> HolidaysFor(string code, int year)
    {
        Catalog.TryGet(CountryCode.Create(code).Value, out HolidayCalendar calendar)
            .ShouldBeTrue($"'{code}' should be an embedded calendar");

        return calendar.ForYear(year);
    }

    [Fact]
    public void Turkey_ProducesItsFixedNationalHolidaysExactly()
    {
        IReadOnlyList<DateOnly> exact =
        [
            .. HolidaysFor("TR", 2026)
                .Where(static holiday => !holiday.IsEstimated)
                .Select(static holiday => holiday.Date)
        ];

        exact.ShouldBe(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 4, 23),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 19),
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 8, 30),
            new DateOnly(2026, 10, 29)
        });
    }

    [Fact]
    public void Turkey_MarksOnlyTheReligiousFeastsAsEstimated()
    {
        foreach (Holiday holiday in HolidaysFor("TR", 2026))
        {
            bool isReligious = holiday.LocalName is "Ramazan Bayramı" or "Kurban Bayramı";

            holiday.IsEstimated.ShouldBe(
                isReligious,
                $"{holiday.LocalName} on {holiday.Date:yyyy-MM-dd}");
        }
    }

    [Theory]
    [InlineData("Ramazan Bayramı", 3)]
    [InlineData("Kurban Bayramı", 4)]
    public void Turkey_ProducesEachFeastAsConsecutiveDays(string localName, int expectedLength)
    {
        IReadOnlyList<DateOnly> dates =
        [
            .. HolidaysFor("TR", 2026)
                .Where(holiday => holiday.LocalName == localName)
                .Select(static holiday => holiday.Date)
                .Order()
        ];

        dates.Count.ShouldBe(expectedLength);

        for (int i = 1; i < dates.Count; i++)
        {
            dates[i].ShouldBe(dates[i - 1].AddDays(1));
        }
    }

    [Theory]
    [InlineData("Ramazan Bayramı", 2026, 3, 20)]
    [InlineData("Kurban Bayramı", 2026, 5, 27)]
    public void Turkey_ReligiousFeastsStayWithinADayOfTheObservedDate(
        string localName,
        int year,
        int month,
        int day)
    {
        DateOnly observed = new(year, month, day);

        DateOnly computed = HolidaysFor("TR", year)
            .Where(holiday => holiday.LocalName == localName)
            .Min(static holiday => holiday.Date);

        int drift = Math.Abs(computed.DayNumber - observed.DayNumber);

        drift.ShouldBeLessThanOrEqualTo(
            1,
            $"{localName} computed as {computed:yyyy-MM-dd}, observed {observed:yyyy-MM-dd}");
    }

    [Fact]
    public void UnitedStates_Produces2026WithTheIndependenceDayObservance() =>
        DatesFor("US", 2026).ShouldBe(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 19),
            new DateOnly(2026, 2, 16),
            new DateOnly(2026, 5, 25),
            new DateOnly(2026, 6, 19),
            new DateOnly(2026, 7, 3),
            new DateOnly(2026, 9, 7),
            new DateOnly(2026, 10, 12),
            new DateOnly(2026, 11, 11),
            new DateOnly(2026, 11, 26),
            new DateOnly(2026, 12, 25)
        });

    [Fact]
    public void UnitedKingdom_Produces2026WithTheBoxingDaySubstitute() =>
        DatesFor("GB", 2026).ShouldBe(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 4, 3),
            new DateOnly(2026, 4, 6),
            new DateOnly(2026, 5, 4),
            new DateOnly(2026, 5, 25),
            new DateOnly(2026, 8, 31),
            new DateOnly(2026, 12, 25),
            new DateOnly(2026, 12, 28)
        });

    [Fact]
    public void Germany_Produces2026WithoutAnySubstitution() =>
        DatesFor("DE", 2026).ShouldBe(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 4, 3),
            new DateOnly(2026, 4, 6),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 25),
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 12, 25),
            new DateOnly(2026, 12, 26)
        });

    [Fact]
    public void Spain_Produces2026AsPublished() =>
        DatesFor("ES", 2026).ShouldBe(new[]
        {
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 6),
            new DateOnly(2026, 4, 3),
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 10, 12),
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 12, 6),
            new DateOnly(2026, 12, 8),
            new DateOnly(2026, 12, 25)
        });

    [Fact]
    public void EveryCalendar_ProducesHolidaysForEverySupportedYearWithoutThrowing()
    {
        foreach (CountrySummary country in Catalog.Countries)
        {
            Catalog.TryGet(country.Code, out HolidayCalendar calendar).ShouldBeTrue();

            for (int year = 1990; year <= 2060; year++)
            {
                IReadOnlyList<Holiday> holidays = calendar.ForYear(year);

                holidays.ShouldNotBeEmpty();
                holidays.ShouldAllBe(holiday => holiday.Date.Year == year);
            }
        }
    }

    [Fact]
    public void HijriAdjustment_ShiftsTheReligiousFeastsAndNothingElse()
    {
        var shifted = new EmbeddedCalendarCatalog(
            Options.Create(new HolidayDataOptions { HijriDayAdjustment = -1 }));

        shifted.TryGet(CountryCode.Create("TR").Value, out HolidayCalendar calendar).ShouldBeTrue();

        IReadOnlyList<Holiday> baseline = HolidaysFor("TR", 2026);
        IReadOnlyList<Holiday> moved = calendar.ForYear(2026);

        DateOnly BaselineStart(string name) => baseline
            .Where(holiday => holiday.LocalName == name)
            .Min(static holiday => holiday.Date);

        DateOnly MovedStart(string name) => moved
            .Where(holiday => holiday.LocalName == name)
            .Min(static holiday => holiday.Date);

        MovedStart("Ramazan Bayramı").ShouldBe(BaselineStart("Ramazan Bayramı").AddDays(-1));
        MovedStart("Kurban Bayramı").ShouldBe(BaselineStart("Kurban Bayramı").AddDays(-1));

        moved
            .Where(static holiday => !holiday.IsEstimated)
            .Select(static holiday => holiday.Date)
            .ShouldBe(baseline
                .Where(static holiday => !holiday.IsEstimated)
                .Select(static holiday => holiday.Date));
    }
}
