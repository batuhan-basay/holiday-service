namespace HolidayService.Application.Contracts;

/// <summary>Breakdown of an inclusive date range into working and non-working days.</summary>
public sealed record WorkdayCountResponse(
    string CountryCode,
    DateOnly From,
    DateOnly To,
    int TotalDays,
    int Workdays,
    int WeekendDays,
    int PublicHolidayDays);

/// <summary>Result of shifting a date by a number of business days.</summary>
public sealed record WorkdayShiftResponse(
    string CountryCode,
    DateOnly StartDate,
    int Workdays,
    DateOnly ResultDate,
    string ResultDayOfWeek);
