namespace HolidayService.Application.Abstractions;

public static class SupportedYears
{
    public const int Min = 1975;

    public const int Max = 2075;

    public static bool Contains(int year) => year is >= Min and <= Max;
}
