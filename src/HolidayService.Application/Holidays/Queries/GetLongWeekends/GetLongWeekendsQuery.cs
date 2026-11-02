using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Holidays.Queries.GetLongWeekends;

public sealed record GetLongWeekendsQuery(string CountryCode, int Year, bool IncludeBridgeDays = true)
    : IQuery<IReadOnlyList<LongWeekendResponse>>;
