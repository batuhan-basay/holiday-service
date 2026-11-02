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

public static class GetDayStatus
{
    public sealed record Query(string CountryCode, DateOnly Date) : IQuery<DayStatusResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Date.Year).MustBeSupportedYear();
        }
    }

    internal sealed class Handler(IHolidayProvider provider)
        : IQueryHandler<Query, DayStatusResponse>
    {
        public async Task<Result<DayStatusResponse>> Handle(
            Query request,
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
}
