namespace HolidayService.Application.Contracts;

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
