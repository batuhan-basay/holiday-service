using HolidayService.Domain.Calendar;

namespace HolidayService.UnitTests.Domain;

public sealed class EasterCalculatorTests
{
    [Theory]
    [InlineData(2020, 4, 12)]
    [InlineData(2021, 4, 4)]
    [InlineData(2022, 4, 17)]
    [InlineData(2023, 4, 9)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2028, 4, 16)]
    [InlineData(2029, 4, 1)]
    [InlineData(2030, 4, 21)]
    [InlineData(2035, 3, 25)]
    public void EasterSunday_MatchesTheKnownDate(int year, int month, int day) =>
        EasterCalculator.EasterSunday(year).ShouldBe(new DateOnly(year, month, day));

    [Fact]
    public void EasterSunday_AlwaysFallsOnASunday()
    {
        for (int year = 1900; year <= 2100; year++)
        {
            EasterCalculator.EasterSunday(year).DayOfWeek.ShouldBe(DayOfWeek.Sunday);
        }
    }

    [Fact]
    public void EasterSunday_StaysInsideItsCanonicalWindow()
    {
        for (int year = 1900; year <= 2100; year++)
        {
            DateOnly easter = EasterCalculator.EasterSunday(year);

            easter.ShouldBeGreaterThanOrEqualTo(new DateOnly(year, 3, 22));
            easter.ShouldBeLessThanOrEqualTo(new DateOnly(year, 4, 25));
        }
    }

    [Fact]
    public void EasterSunday_RejectsPreGregorianYears() =>
        Should.Throw<ArgumentOutOfRangeException>(() => EasterCalculator.EasterSunday(1500));
}
