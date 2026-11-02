using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Countries.Queries.GetCountries;

public sealed record GetCountriesQuery : IQuery<IReadOnlyList<CountryResponse>>;
