using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries;

public static class GetUpcomingHolidays
{
    public const int MaxDays = 365;

    public sealed record Query(string CountryCode, int Days = 30)
        : IQuery<HolidayCollectionResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Days)
                .InclusiveBetween(1, MaxDays)
                .WithMessage($"Days must be between 1 and {MaxDays}.");
        }
    }

    internal sealed class Handler(HolidayRangeLoader loader, IDateTimeProvider clock)
        : IQueryHandler<Query, HolidayCollectionResponse>
    {
        public async Task<Result<HolidayCollectionResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

            if (countryCode.IsFailure)
            {
                return Result.Failure<HolidayCollectionResponse>(countryCode.Error);
            }

            DateOnly today = clock.Today;
            DateOnly until = today.AddDays(request.Days);

            Result<IReadOnlyList<Holiday>> holidays = await loader
                .LoadAsync(countryCode.Value, today, until, cancellationToken)
                .ConfigureAwait(false);

            if (holidays.IsFailure)
            {
                return Result.Failure<HolidayCollectionResponse>(holidays.Error);
            }

            IReadOnlyList<HolidayResponse> upcoming = holidays.Value.ToResponse();

            return Result.Success(new HolidayCollectionResponse(
                countryCode.Value.Value,
                today,
                until,
                upcoming.Count,
                upcoming));
        }
    }
}
