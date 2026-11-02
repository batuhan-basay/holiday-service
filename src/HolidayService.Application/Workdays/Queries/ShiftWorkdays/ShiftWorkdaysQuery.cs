using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Workdays.Queries.ShiftWorkdays;

public sealed record ShiftWorkdaysQuery(string CountryCode, DateOnly StartDate, int Workdays)
    : IQuery<WorkdayShiftResponse>
{
    public const int MaxWorkdays = 500;
}
