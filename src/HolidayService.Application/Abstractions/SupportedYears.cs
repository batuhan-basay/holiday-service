namespace HolidayService.Application.Abstractions;

/// <summary>The year window the upstream provider publishes data for.</summary>
public static class SupportedYears
{
    public const int Min = 1975;

    public const int Max = 2075;

    public static bool Contains(int year) => year is >= Min and <= Max;
}
