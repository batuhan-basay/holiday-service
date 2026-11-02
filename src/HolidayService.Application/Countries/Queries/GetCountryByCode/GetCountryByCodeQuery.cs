using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Contracts;

namespace HolidayService.Application.Countries.Queries.GetCountryByCode;

public sealed record GetCountryByCodeQuery(string CountryCode) : IQuery<CountryDetailResponse>;
