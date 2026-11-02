using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries;

public static class GetHolidaysByRange
{
    public const int MaxRangeDays = 1830;

    public sealed record Query(string CountryCode, DateOnly From, DateOnly To)
        : IQuery<HolidayCollectionResponse>;

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

            int days = request.To.DayNumber - request.From.DayNumber + 1;

            if (days > MaxRangeDays)
            {
                return Result.Failure<HolidayCollectionResponse>(
                    HolidayErrors.RangeTooWide(days, MaxRangeDays));
            }

            Result<IReadOnlyList<Holiday>> holidays = await loader
                .LoadAsync(countryCode.Value, request.From, request.To, cancellationToken)
                .ConfigureAwait(false);

            if (holidays.IsFailure)
            {
                return Result.Failure<HolidayCollectionResponse>(holidays.Error);
            }

            IReadOnlyList<HolidayResponse> response = holidays.Value.ToResponse();

            return Result.Success(new HolidayCollectionResponse(
                countryCode.Value.Value,
                request.From,
                request.To,
                response.Count,
                response));
        }
    }
}
