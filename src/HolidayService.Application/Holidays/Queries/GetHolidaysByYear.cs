using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Holidays.Queries;

/// <summary>Every holiday published for one country in one calendar year.</summary>
public static class GetHolidaysByYear
{
    public sealed record Query(
        string CountryCode,
        int Year,
        string? County = null,
        HolidayType? Type = null) : IQuery<HolidayCollectionResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(query => query.CountryCode).MustBeCountryCode();
            RuleFor(query => query.Year).MustBeSupportedYear();
            RuleFor(query => query.County)
                .MaximumLength(10)
                .When(query => query.County is not null);
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

            IReadOnlyList<HolidayResponse> filtered = holidays.Value
                .Apply(request.County, request.Type)
                .OrderBy(static holiday => holiday.Date)
                .ToResponse();

            return Result.Success(new HolidayCollectionResponse(
                countryCode.Value.Value,
                new DateOnly(request.Year, 1, 1),
                new DateOnly(request.Year, 12, 31),
                filtered.Count,
                filtered));
        }
    }
}
