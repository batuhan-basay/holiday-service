using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Workdays.Queries;

public static class CountWorkdays
{
    public const int MaxRangeDays = 1830;

    public sealed record Query(string CountryCode, DateOnly From, DateOnly To)
        : IQuery<WorkdayCountResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.From.Year).MustBeSupportedYear();
            RuleFor(query => query.To.Year).MustBeSupportedYear();

            RuleFor(query => query)
                .Must(query => query.From <= query.To)
                .WithName("to")
                .WithMessage("'from' must be earlier than or equal to 'to'.");

            RuleFor(query => query)
                .Must(query => query.To.DayNumber - query.From.DayNumber + 1 <= MaxRangeDays)
                .WithName("to")
                .WithMessage($"The requested range may not exceed {MaxRangeDays} days.");
        }
    }

    internal sealed class Handler(HolidayRangeLoader loader)
        : IQueryHandler<Query, WorkdayCountResponse>
    {
        public async Task<Result<WorkdayCountResponse>> Handle(
            Query request,
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

            if (totalDays > MaxRangeDays)
            {
                return Result.Failure<WorkdayCountResponse>(
                    HolidayErrors.RangeTooWide(totalDays, MaxRangeDays));
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
}
