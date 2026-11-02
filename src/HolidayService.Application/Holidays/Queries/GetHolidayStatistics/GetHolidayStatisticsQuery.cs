using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetHolidayStatistics;

public sealed record GetHolidayStatisticsQuery(string CountryCode, int Year) : IQuery<HolidayStatisticsResponse>;
