using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByYear;

public sealed record GetHolidaysByYearQuery(
    string CountryCode,
    int Year,
    HolidayType? Type = null) : IQuery<HolidayCollectionResponse>;
