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

/// <summary>
/// Stretches of consecutive days off in a year. Computed locally rather than taken from the
/// upstream provider so the country's own weekend definition is honoured — the upstream feed
/// assumes a Saturday/Sunday weekend everywhere.
/// </summary>
public static class GetLongWeekends
{
    public sealed record Query(string CountryCode, int Year, bool IncludeBridgeDays = true)
        : IQuery<IReadOnlyList<LongWeekendResponse>>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Year).MustBeSupportedYear();
        }
    }

    internal sealed class Handler(IHolidayProvider provider)
        : IQueryHandler<Query, IReadOnlyList<LongWeekendResponse>>
    {
        public async Task<Result<IReadOnlyList<LongWeekendResponse>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

            if (countryCode.IsFailure)
            {
                return Result.Failure<IReadOnlyList<LongWeekendResponse>>(countryCode.Error);
            }

            Result<IReadOnlyList<Holiday>> holidays = await provider
                .GetHolidaysAsync(countryCode.Value, request.Year, cancellationToken)
                .ConfigureAwait(false);

            if (holidays.IsFailure)
            {
                return Result.Failure<IReadOnlyList<LongWeekendResponse>>(holidays.Error);
            }

            var calendar = new WorkCalendar(countryCode.Value, holidays.Value);

            IReadOnlyList<LongWeekendResponse> response =
            [
                .. calendar
                    .FindLongWeekends(request.Year, request.IncludeBridgeDays)
                    .Select(static weekend => weekend.ToResponse())
            ];

            return Result.Success(response);
        }
    }
}
