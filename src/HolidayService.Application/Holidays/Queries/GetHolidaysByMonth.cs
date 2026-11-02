using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries;

public static class GetHolidaysByMonth
{
    public sealed record Query(string CountryCode, int Year, int Month)
        : IQuery<HolidayCollectionResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Year).MustBeSupportedYear();
            RuleFor(query => query.Month)
                .InclusiveBetween(1, 12)
                .WithMessage("Month must be between 1 and 12.");
        }
    }

    internal sealed class Handler(IHolidayProvider provider)
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

            Result<IReadOnlyList<Holiday>> holidays = await provider
                .GetHolidaysAsync(countryCode.Value, request.Year, cancellationToken)
                .ConfigureAwait(false);

            if (holidays.IsFailure)
            {
                return Result.Failure<HolidayCollectionResponse>(holidays.Error);
            }

            var from = new DateOnly(request.Year, request.Month, 1);
            var to = new DateOnly(
                request.Year,
                request.Month,
                DateTime.DaysInMonth(request.Year, request.Month));

            IReadOnlyList<HolidayResponse> inMonth = holidays.Value
                .Where(holiday => holiday.Date >= from && holiday.Date <= to)
                .OrderBy(static holiday => holiday.Date)
                .ToResponse();

            return Result.Success(new HolidayCollectionResponse(
                countryCode.Value.Value,
                from,
                to,
                inMonth.Count,
                inMonth));
        }
    }
}
