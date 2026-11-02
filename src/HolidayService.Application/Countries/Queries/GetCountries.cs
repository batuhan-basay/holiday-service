using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;

namespace HolidayService.Application.Countries.Queries;

/// <summary>Every country the service can return holidays for.</summary>
public static class GetCountries
{
    public sealed record Query : IQuery<IReadOnlyList<CountryResponse>>;

    internal sealed class Handler(IHolidayProvider provider)
        : IQueryHandler<Query, IReadOnlyList<CountryResponse>>
    {
        public async Task<Result<IReadOnlyList<CountryResponse>>> Handle(
            Query request,
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
}
