using FluentValidation;
using HolidayService.Application.Abstractions;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;

namespace HolidayService.Application.Countries.Queries;

/// <summary>Full profile for one country, including its land borders.</summary>
public static class GetCountryByCode
{
    public sealed record Query(string CountryCode) : IQuery<CountryDetailResponse>;

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(query => query.CountryCode).MustBeCountryCode();
    }

    internal sealed class Handler(IHolidayProvider provider)
        : IQueryHandler<Query, CountryDetailResponse>
    {
        public async Task<Result<CountryDetailResponse>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            Result<CountryCode> countryCode = CountryCode.Create(request.CountryCode);

            if (countryCode.IsFailure)
            {
                return Result.Failure<CountryDetailResponse>(countryCode.Error);
            }

            Result<Country> country = await provider
                .GetCountryAsync(countryCode.Value, cancellationToken)
                .ConfigureAwait(false);

            return country.Map(static value => value.ToResponse());
        }
    }
}
