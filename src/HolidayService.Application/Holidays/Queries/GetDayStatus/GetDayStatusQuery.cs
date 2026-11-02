using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetDayStatus;

public sealed record GetDayStatusQuery(string CountryCode, DateOnly Date) : IQuery<DayStatusResponse>;
