namespace HolidayService.Application.Contracts;

public sealed record WorkdayCountResponse(
    string CountryCode,
    DateOnly From,
    DateOnly To,
    int TotalDays,
    int Workdays,
    int WeekendDays,
    int PublicHolidayDays);

public sealed record WorkdayShiftResponse(
    string CountryCode,
    DateOnly StartDate,
    int Workdays,
    DateOnly ResultDate,
    string ResultDayOfWeek);
