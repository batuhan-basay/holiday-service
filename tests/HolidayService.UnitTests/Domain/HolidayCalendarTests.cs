using HolidayService.Domain.Calendar;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.UnitTests.Domain;

public sealed class HolidayCalendarTests
{
    private static readonly CountryCode Code = CountryCode.Create("ZZ").Value;

    private static HolidayCalendar Calendar(params HolidayRule[] rules) =>
        new(Code, "Testland", "Republic of Testland", WeekendDefinition.SaturdaySunday, rules);

    private static FixedDateRule Fixed(
        int month,
        int day,
        SubstitutionPolicy substitution = SubstitutionPolicy.None,
        string name = "Holiday") => new()
        {
            Name = name,
            LocalName = name,
            Month = month,
            Day = day,
            Substitution = substitution
        };

    [Fact]
    public void ForYear_OrdersHolidaysByDate()
    {
        HolidayCalendar calendar = Calendar(
            Fixed(12, 25, name: "Christmas"),
            Fixed(1, 1, name: "New Year"));

        IReadOnlyList<Holiday> holidays = calendar.ForYear(2026);

        holidays.Select(static holiday => holiday.Name)
            .ShouldBe(new[] { "New Year", "Christmas" });
    }

    [Fact]
    public void ForYear_LeavesWeekendHolidaysAloneWithoutASubstitutionPolicy()
    {
        HolidayCalendar calendar = Calendar(Fixed(8, 15));

        Holiday holiday = calendar.ForYear(2026).ShouldHaveSingleItem();

        holiday.Date.ShouldBe(new DateOnly(2026, 8, 15));
        holiday.IsSubstitute.ShouldBeFalse();
    }

    [Fact]
    public void ForYear_NextWorkdayMovesASaturdayHolidayForward()
    {
        HolidayCalendar calendar = Calendar(Fixed(12, 26, SubstitutionPolicy.NextWorkday));

        Holiday holiday = calendar.ForYear(2026).ShouldHaveSingleItem();

        holiday.Date.ShouldBe(new DateOnly(2026, 12, 28));
        holiday.IsSubstitute.ShouldBeTrue();
    }

    [Fact]
    public void ForYear_NearestWorkdayPullsASaturdayHolidayBack()
    {
        HolidayCalendar calendar = Calendar(Fixed(7, 4, SubstitutionPolicy.NearestWorkday));

        Holiday holiday = calendar.ForYear(2026).ShouldHaveSingleItem();

        holiday.Date.ShouldBe(new DateOnly(2026, 7, 3));
        holiday.IsSubstitute.ShouldBeTrue();
    }

    [Fact]
    public void ForYear_NearestWorkdayPushesASundayHolidayForward()
    {
        HolidayCalendar calendar = Calendar(Fixed(11, 1, SubstitutionPolicy.NearestWorkday));

        Holiday holiday = calendar.ForYear(2026).ShouldHaveSingleItem();

        holiday.Date.ShouldBe(new DateOnly(2026, 11, 2));
        holiday.IsSubstitute.ShouldBeTrue();
    }

    [Fact]
    public void ForYear_SubstitutesDoNotLandOnAnAlreadyOccupiedDay()
    {
        HolidayCalendar calendar = Calendar(
            Fixed(12, 26, SubstitutionPolicy.NextWorkday, "Boxing Day"),
            Fixed(12, 27, SubstitutionPolicy.NextWorkday, "Extra Day"));

        IReadOnlyList<Holiday> holidays = calendar.ForYear(2026);

        holidays.Select(static holiday => holiday.Date)
            .ShouldBe(new[] { new DateOnly(2026, 12, 28), new DateOnly(2026, 12, 29) });
        holidays.ShouldAllBe(holiday => holiday.IsSubstitute);
    }

    [Fact]
    public void ForYear_KeepsSubstitutesOutOfTheNeighbouringYear()
    {
        HolidayCalendar calendar = Calendar(Fixed(12, 31, SubstitutionPolicy.NextWorkday));

        calendar.ForYear(2026).ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void ToCountry_ExposesTheWeekendDefinition()
    {
        Country country = Calendar().ToCountry();

        country.Code.ShouldBe(Code);
        country.Name.ShouldBe("Testland");
        country.OfficialName.ShouldBe("Republic of Testland");
        country.WeekendDays.ShouldBe(new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, ignoreOrder: true);
    }

    [Fact]
    public void Constructor_FallsBackToTheCommonNameWhenNoOfficialNameIsGiven()
    {
        var calendar = new HolidayCalendar(
            Code,
            "Testland",
            string.Empty,
            WeekendDefinition.SaturdaySunday,
            []);

        calendar.OfficialName.ShouldBe("Testland");
    }
}
