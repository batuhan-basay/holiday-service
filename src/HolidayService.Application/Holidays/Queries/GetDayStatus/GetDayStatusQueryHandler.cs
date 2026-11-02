using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries.GetDayStatus;

internal sealed class GetDayStatusQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetDayStatusQuery, DayStatusResponse>
{
    public async Task<Result<DayStatusResponse>> Handle(
        GetDayStatusQuery request,
        CancellationToken cancellationToken)
    {
        Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

        if (countryCode.IsFailure)
        {
            return Result.Failure<DayStatusResponse>(countryCode.Error);
        }

        Result<IReadOnlyList<Holiday>> holidays = await provider
            .GetHolidaysAsync(countryCode.Value, request.Date.Year, cancellationToken)
            .ConfigureAwait(false);

        if (holidays.IsFailure)
        {
            return Result.Failure<DayStatusResponse>(holidays.Error);
        }

        var calendar = new WorkCalendar(countryCode.Value, holidays.Value);
        Holiday? holiday = calendar.HolidayOn(request.Date);

        return Result.Success(new DayStatusResponse(
            countryCode.Value.Value,
            request.Date,
            request.Date.DayOfWeek.ToString(),
            holiday is not null,
            calendar.IsWeekend(request.Date),
            calendar.IsWorkday(request.Date),
            holiday?.ToResponse()));
    }
}
