using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByRange;

public sealed record GetHolidaysByRangeQuery(string CountryCode, DateOnly From, DateOnly To)
    : IQuery<HolidayCollectionResponse>
{
    public const int MaxRangeDays = 1830;
}
