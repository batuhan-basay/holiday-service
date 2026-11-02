using HolidayService.Domain.Holidays;

namespace HolidayService.UnitTests.Domain;

public sealed class HolidayRuleTests
{
    private static FixedDateRule Fixed(int month, int day) => new()
    {
        Name = "Fixed",
        LocalName = "Fixed",
        Month = month,
        Day = day
    };

    private static NthWeekdayRule Nth(int month, DayOfWeek day, int occurrence) => new()
    {
        Name = "Nth",
        LocalName = "Nth",
        Month = month,
        DayOfWeek = day,
        Occurrence = occurrence
    };

    [Fact]
    public void FixedDateRule_YieldsThatDate() =>
        Fixed(10, 29).OccurrencesIn(2026).ShouldBe(new[] { new DateOnly(2026, 10, 29) });

    [Fact]
    public void FixedDateRule_SkipsFebruary29InACommonYear()
    {
        Fixed(2, 29).OccurrencesIn(2026).ShouldBeEmpty();
        Fixed(2, 29).OccurrencesIn(2028).ShouldBe(new[] { new DateOnly(2028, 2, 29) });
    }

    [Theory]
    [InlineData(1, 3, 2026, 1, 19)]
    [InlineData(2, 3, 2026, 2, 16)]
    [InlineData(9, 1, 2026, 9, 7)]
    [InlineData(10, 2, 2026, 10, 12)]
    public void NthWeekdayRule_FindsTheNthMonday(
        int month,
        int occurrence,
        int year,
        int expectedMonth,
        int expectedDay) =>
        Nth(month, DayOfWeek.Monday, occurrence)
            .OccurrencesIn(year)
            .ShouldBe(new[] { new DateOnly(year, expectedMonth, expectedDay) });

    [Fact]
    public void NthWeekdayRule_MinusOneMeansTheLastOccurrence()
    {
        Nth(5, DayOfWeek.Monday, -1)
            .OccurrencesIn(2026)
            .ShouldBe(new[] { new DateOnly(2026, 5, 25) });

        Nth(8, DayOfWeek.Monday, -1)
            .OccurrencesIn(2026)
            .ShouldBe(new[] { new DateOnly(2026, 8, 31) });
    }

    [Fact]
    public void NthWeekdayRule_FindsTheFourthThursday() =>
        Nth(11, DayOfWeek.Thursday, 4)
            .OccurrencesIn(2026)
            .ShouldBe(new[] { new DateOnly(2026, 11, 26) });

    [Fact]
    public void NthWeekdayRule_YieldsNothingWhenTheMonthHasNoFifthOccurrence() =>
        Nth(2, DayOfWeek.Monday, 5).OccurrencesIn(2026).ShouldBeEmpty();

    [Fact]
    public void EasterOffsetRule_DerivesGoodFridayAndEasterMonday()
    {
        var goodFriday = new EasterOffsetRule
        {
            Name = "Good Friday",
            LocalName = "Good Friday",
            OffsetDays = -2
        };

        var easterMonday = new EasterOffsetRule
        {
            Name = "Easter Monday",
            LocalName = "Easter Monday",
            OffsetDays = 1
        };

        goodFriday.OccurrencesIn(2026).ShouldBe(new[] { new DateOnly(2026, 4, 3) });
        easterMonday.OccurrencesIn(2026).ShouldBe(new[] { new DateOnly(2026, 4, 6) });
    }

    [Fact]
    public void HijriRule_ProducesConsecutiveDaysInsideTheYear()
    {
        var ramadanFeast = new HijriRule
        {
            Name = "Ramadan Feast",
            LocalName = "Ramazan Bayramı",
            HijriMonth = 10,
            HijriDay = 1,
            Length = 3
        };

        IReadOnlyList<DateOnly> dates = [.. ramadanFeast.OccurrencesIn(2026)];

        dates.Count.ShouldBe(3);
        dates.ShouldAllBe(date => date.Year == 2026);

        for (int i = 1; i < dates.Count; i++)
        {
            dates[i].ShouldBe(dates[i - 1].AddDays(1));
        }
    }

    [Fact]
    public void HijriRule_IsMarkedEstimatedAndOthersAreNot()
    {
        new HijriRule
        {
            Name = "x",
            LocalName = "x",
            HijriMonth = 10,
            HijriDay = 1
        }.IsEstimated.ShouldBeTrue();

        Fixed(1, 1).IsEstimated.ShouldBeFalse();
        Nth(1, DayOfWeek.Monday, 1).IsEstimated.ShouldBeFalse();
    }
}
