using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries;

public static class GetHolidayStatistics
{
    public sealed record Query(string CountryCode, int Year) : IQuery<HolidayStatisticsResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Year).MustBeSupportedYear();
        }
    }

    internal sealed class Handler(IHolidayProvider provider)
        : IQueryHandler<Query, HolidayStatisticsResponse>
    {
        public async Task<Result<HolidayStatisticsResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

            if (countryCode.IsFailure)
            {
                return Result.Failure<HolidayStatisticsResponse>(countryCode.Error);
            }

            Result<IReadOnlyList<Holiday>> holidays = await provider
                .GetHolidaysAsync(countryCode.Value, request.Year, cancellationToken)
                .ConfigureAwait(false);

            if (holidays.IsFailure)
            {
                return Result.Failure<HolidayStatisticsResponse>(holidays.Error);
            }

            var calendar = new WorkCalendar(countryCode.Value, holidays.Value);

            IReadOnlyList<Holiday> publicHolidays =
            [
                .. holidays.Value
                    .Where(static holiday => holiday.IsPublic)
                    .DistinctBy(static holiday => holiday.Date)
            ];

            int onWeekend = publicHolidays.Count(holiday => calendar.IsWeekend(holiday.Date));

            IReadOnlyList<MonthHolidayCount> byMonth =
            [
                .. Enumerable.Range(1, 12)
                    .Select(month => new MonthHolidayCount(
                        month,
                        HolidayMapper.ToMonthName(month),
                        publicHolidays.Count(holiday => holiday.Date.Month == month)))
            ];

            IReadOnlyList<WeekdayHolidayCount> byDayOfWeek =
            [
                .. Enum.GetValues<DayOfWeek>()
                    .Select(day => new WeekdayHolidayCount(
                        day.ToString(),
                        publicHolidays.Count(holiday => holiday.DayOfWeek == day)))
            ];

            return Result.Success(new HolidayStatisticsResponse(
                countryCode.Value.Value,
                request.Year,
                publicHolidays.Count,
                onWeekend,
                publicHolidays.Count - onWeekend,
                calendar.FindLongWeekends(request.Year, includeBridgeDays: false).Count,
                byMonth,
                byDayOfWeek));
        }
    }
}
