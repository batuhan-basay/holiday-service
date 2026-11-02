using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetWorldwideUpcomingHolidays;

public sealed record GetWorldwideUpcomingHolidaysQuery : IQuery<IReadOnlyList<HolidayResponse>>;
