using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByMonth;

public sealed record GetHolidaysByMonthQuery(string CountryCode, int Year, int Month)
    : IQuery<HolidayCollectionResponse>;
