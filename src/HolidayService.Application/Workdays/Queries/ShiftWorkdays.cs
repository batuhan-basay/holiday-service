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

/// <summary>
/// Moves a date forward or backward by a number of business days, skipping weekends and
/// public holidays. Negative values walk backwards.
/// </summary>
public static class ShiftWorkdays
{
    public const int MaxWorkdays = 500;

    public sealed record Query(string CountryCode, DateOnly StartDate, int Workdays)
        : IQuery<WorkdayShiftResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.StartDate.Year).MustBeSupportedYear();
            RuleFor(query => query.Workdays)
                .InclusiveBetween(-MaxWorkdays, MaxWorkdays)
                .WithMessage($"Workdays must be between -{MaxWorkdays} and {MaxWorkdays}.");
        }
    }

    internal sealed class Handler(HolidayRangeLoader loader)
        : IQueryHandler<Query, WorkdayShiftResponse>
    {
        public async Task<Result<WorkdayShiftResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

            if (countryCode.IsFailure)
            {
                return Result.Failure<WorkdayShiftResponse>(countryCode.Error);
            }

            // Load a calendar window wide enough that the walk can never run past the
            // holidays we know about: every business day costs at most ~3 calendar days.
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
}
