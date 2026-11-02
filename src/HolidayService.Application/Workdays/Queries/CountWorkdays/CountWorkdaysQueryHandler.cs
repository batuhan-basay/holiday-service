using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Workdays.Queries.CountWorkdays;

internal sealed class CountWorkdaysQueryHandler(HolidayRangeLoader loader)
    : IQueryHandler<CountWorkdaysQuery, WorkdayCountResponse>
{
    public async Task<Result<WorkdayCountResponse>> Handle(
        CountWorkdaysQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<WorkdayCountResponse>(countryCode.Error);
        }

        int totalDays = request.To.DayNumber - request.From.DayNumber + 1;

        if (totalDays <= 0)
        {
            return Result.Failure<WorkdayCountResponse>(HolidayErrors.RangeInverted);
        }

        if (totalDays > CountWorkdaysQuery.MaxRangeDays)
        {
            return Result.Failure<WorkdayCountResponse>(
                HolidayErrors.RangeTooWide(totalDays, CountWorkdaysQuery.MaxRangeDays));
        }

        Result<IReadOnlyList<Holiday>> holidays = await loader
            .LoadAsync(countryCode.Value, request.From, request.To, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<WorkdayCountResponse>(holidays.Error);
        }

        var calendar = new WorkCalendar(countryCode.Value, holidays.Value);

        int weekendDays = 0;
        int holidayDays = 0;
        int workdays = 0;

        for (DateOnly day = request.From; day <= request.To; day = day.AddDays(1))
        {
            bool isWeekend = calendar.IsWeekend(day);
            bool isHoliday = calendar.IsPublicHoliday(day);

            if (isWeekend)
            {
                weekendDays++;
            }

            if (isHoliday)
            {
                holidayDays++;
            }

            if (!isWeekend && !isHoliday)
            {
                workdays++;
            }
        }

        return Result.Success(new WorkdayCountResponse(
            countryCode.Value.Value,
            request.From,
            request.To,
            totalDays,
            workdays,
            weekendDays,
            holidayDays));
    }
}
