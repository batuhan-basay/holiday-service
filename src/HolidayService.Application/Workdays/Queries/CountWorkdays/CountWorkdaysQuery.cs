using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Workdays.Queries.CountWorkdays;

public sealed record CountWorkdaysQuery(string CountryCode, DateOnly From, DateOnly To)
    : IQuery<WorkdayCountResponse>
{
    public const int MaxRangeDays = 1830;
}
