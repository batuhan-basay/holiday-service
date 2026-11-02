using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;

namespace HolidayService.Application.Countries.Queries.GetCountries;

internal sealed class GetCountriesQueryHandler(IHolidayProvider provider)
    : IQueryHandler<GetCountriesQuery, IReadOnlyList<CountryResponse>>
{
    public async Task<Result<IReadOnlyList<CountryResponse>>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CountrySummary>> countries = await provider
            .GetAvailableCountriesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (countries.IsFailure)
        {
            return Result.Failure<IReadOnlyList<CountryResponse>>(countries.Error);
        }

        IReadOnlyList<CountryResponse> response =
        [
            .. countries.Value
                .OrderBy(static country => country.Name, StringComparer.Ordinal)
                .Select(static country => country.ToResponse())
        ];

        return Result.Success(response);
    }
}
