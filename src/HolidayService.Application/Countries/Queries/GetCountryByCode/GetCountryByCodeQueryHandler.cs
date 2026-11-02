using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;

namespace HolidayService.Application.Countries.Queries.GetCountryByCode;

internal sealed class GetCountryByCodeQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetCountryByCodeQuery, CountryDetailResponse>
{
    public async Task<Result<CountryDetailResponse>> Handle(
        GetCountryByCodeQuery request,
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
