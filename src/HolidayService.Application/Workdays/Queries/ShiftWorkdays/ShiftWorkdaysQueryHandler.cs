using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Workdays.Queries.ShiftWorkdays;

internal sealed class ShiftWorkdaysQueryHandler(HolidayRangeLoader loader)
    : IQueryHandler<ShiftWorkdaysQuery, WorkdayShiftResponse>
{
    public async Task<Result<WorkdayShiftResponse>> Handle(
        ShiftWorkdaysQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<WorkdayShiftResponse>(countryCode.Error);
        }

        int span = (Math.Abs(request.Workdays) * 3) + 14;

        DateOnly windowStart = request.Workdays >= 0
            ? request.StartDate
            : request.StartDate.AddDays(-span);

        DateOnly windowEnd = request.Workdays >= 0
            ? request.StartDate.AddDays(span)
            : request.StartDate;

        Result<IReadOnlyList<Holiday>> holidays = await loader
            .LoadAsync(countryCode.Value, windowStart, windowEnd, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<WorkdayShiftResponse>(holidays.Error);
        }

        var calendar = new WorkCalendar(countryCode.Value, holidays.Value);
        DateOnly result = calendar.AddWorkdays(request.StartDate, request.Workdays);

        return Result.Success(new WorkdayShiftResponse(
            countryCode.Value.Value,
            request.StartDate,
            request.Workdays,
            result,
            result.DayOfWeek.ToString()));
    }
}
