using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetNextHolidays;

public sealed record GetNextHolidaysQuery(string CountryCode, int Count = 5)
    : IQuery<HolidayCollectionResponse>
{
    public const int MaxCount = 50;
}
