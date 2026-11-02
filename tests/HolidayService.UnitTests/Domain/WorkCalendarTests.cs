using HolidayService.Domain.Calendar;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;
using HolidayService.UnitTests.TestData;

namespace HolidayService.UnitTests.Domain;

public sealed class WorkCalendarTests
{
    // 2026-01-01 is a Thursday; 2026-01-03/04 is the first weekend of that year.
    private static WorkCalendar TurkishCalendar(params Holiday[] holidays) =>
        new(HolidayFactory.Turkey, holidays);

    [Fact]
    public void IsWorkday_FalseOnWeekend()
    {
        WorkCalendar calendar = TurkishCalendar();

        calendar.IsWorkday(new DateOnly(2026, 1, 3)).ShouldBeFalse();
        calendar.IsWorkday(new DateOnly(2026, 1, 4)).ShouldBeFalse();
        calendar.IsWorkday(new DateOnly(2026, 1, 5)).ShouldBeTrue();
    }

    [Fact]
    public void IsWorkday_FalseOnPublicHoliday()
    {
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 1, "New Year"));

        calendar.IsPublicHoliday(new DateOnly(2026, 1, 1)).ShouldBeTrue();
        calendar.IsWorkday(new DateOnly(2026, 1, 1)).ShouldBeFalse();
    }

    [Fact]
    public void NonPublicHolidayTypes_DoNotBlockWork()
    {
        WorkCalendar calendar = TurkishCalendar(
            HolidayFactory.On(2026, 2, 5, "Observance", type: HolidayType.Observance));

        calendar.IsWorkday(new DateOnly(2026, 2, 5)).ShouldBeTrue();
    }

    [Fact]
    public void CountWorkdays_ExcludesWeekendsAndHolidays()
    {
        // Mon 2026-01-05 .. Sun 2026-01-11 => 5 weekdays, one of which is a holiday.
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 7, "Mid-week"));

        calendar.CountWorkdays(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11)).ShouldBe(4);
    }

    [Fact]
    public void CountWorkdays_IsInclusiveOnBothEnds()
    {
        WorkCalendar calendar = TurkishCalendar();

        calendar.CountWorkdays(new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5)).ShouldBe(1);
    }

    [Fact]
    public void CountWorkdays_ReturnsZeroForInvertedRange()
    {
        WorkCalendar calendar = TurkishCalendar();

        calendar.CountWorkdays(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 5)).ShouldBe(0);
    }

    [Fact]
    public void AddWorkdays_SkipsWeekend()
    {
        WorkCalendar calendar = TurkishCalendar();

        // Friday + 1 workday => Monday.
        calendar
            .AddWorkdays(new DateOnly(2026, 1, 2), 1)
            .ShouldBe(new DateOnly(2026, 1, 5));
    }

    [Fact]
    public void AddWorkdays_SkipsHolidays()
    {
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 6, "Holiday"));

        // Monday + 1 workday would be Tuesday, but Tuesday is a holiday => Wednesday.
        calendar
            .AddWorkdays(new DateOnly(2026, 1, 5), 1)
            .ShouldBe(new DateOnly(2026, 1, 7));
    }

    [Fact]
    public void AddWorkdays_ZeroReturnsTheStartDateUnchanged()
    {
        WorkCalendar calendar = TurkishCalendar();
        var start = new DateOnly(2026, 1, 3);

        calendar.AddWorkdays(start, 0).ShouldBe(start);
    }

    [Fact]
    public void AddWorkdays_NegativeWalksBackwards()
    {
        WorkCalendar calendar = TurkishCalendar();

        // Monday - 1 workday => the preceding Friday.
        calendar
            .AddWorkdays(new DateOnly(2026, 1, 5), -1)
            .ShouldBe(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public void Weekend_FollowsTheCountryDefinition()
    {
        var saudi = new WorkCalendar(CountryCode.Create("SA").Value, []);

        // Saudi Arabia rests on Friday and Saturday: 2026-01-02 is a Friday,
        // 2026-01-04 a Sunday and therefore a working day there.
        saudi.IsWeekend(new DateOnly(2026, 1, 2)).ShouldBeTrue();
        saudi.IsWeekend(new DateOnly(2026, 1, 4)).ShouldBeFalse();
        saudi.Weekend.ShouldBe(WeekendDefinition.FridaySaturday);
    }

    [Fact]
    public void FindLongWeekends_DetectsThreeDayStretch()
    {
        // Friday 2026-01-02 off => Fri+Sat+Sun is a three-day stretch.
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 2, "Bridge"));

        IReadOnlyList<LongWeekend> weekends = calendar.FindLongWeekends(2026);

        weekends.ShouldContain(weekend =>
            weekend.StartDate == new DateOnly(2026, 1, 2)
            && weekend.EndDate == new DateOnly(2026, 1, 4)
            && weekend.DayCount == 3
            && !weekend.NeedsBridgeDay);
    }

    [Fact]
    public void FindLongWeekends_FlagsBridgeDayOpportunities()
    {
        // Tuesday 2026-01-06 off: taking Monday 01-05 bridges back to the weekend.
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 6, "Tuesday off"));

        IReadOnlyList<LongWeekend> weekends = calendar.FindLongWeekends(2026);

        weekends.ShouldContain(weekend => weekend.NeedsBridgeDay);
    }

    [Fact]
    public void FindLongWeekends_CanExcludeBridgeDays()
    {
        WorkCalendar calendar = TurkishCalendar(HolidayFactory.On(2026, 1, 6, "Tuesday off"));

        IReadOnlyList<LongWeekend> weekends =
            calendar.FindLongWeekends(2026, includeBridgeDays: false);

        weekends.ShouldAllBe(weekend => !weekend.NeedsBridgeDay);
    }

    [Fact]
    public void DuplicateHolidayDates_AreIndexedOnce()
    {
        WorkCalendar calendar = TurkishCalendar(
            HolidayFactory.On(2026, 5, 1, "Labour Day"),
            HolidayFactory.On(2026, 5, 1, "Spring Day"));

        calendar.HolidayOn(new DateOnly(2026, 5, 1)).ShouldNotBeNull();
    }
}
