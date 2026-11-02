namespace HolidayService.Application.Contracts;

/// <summary>Aggregated view of a country's holiday year — useful for dashboards.</summary>
public sealed record HolidayStatisticsResponse(
    string CountryCode,
    int Year,
    int TotalHolidays,
    int FallingOnWeekend,
    int EffectiveDaysOff,
    int LongWeekendCount,
    IReadOnlyList<MonthHolidayCount> ByMonth,
    IReadOnlyList<WeekdayHolidayCount> ByDayOfWeek);

public sealed record MonthHolidayCount(int Month, string MonthName, int Count);

public sealed record WeekdayHolidayCount(string DayOfWeek, int Count);
