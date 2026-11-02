using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetUpcomingHolidays;

public sealed record GetUpcomingHolidaysQuery(string CountryCode, int Days = 30)
    : IQuery<HolidayCollectionResponse>
{
    public const int MaxDays = 365;
}
