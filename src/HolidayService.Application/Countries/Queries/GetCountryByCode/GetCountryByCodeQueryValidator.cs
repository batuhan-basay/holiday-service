using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Countries.Queries.GetCountryByCode;

internal sealed class GetCountryByCodeQueryValidator : AbstractValidator<GetCountryByCodeQuery>
{
    public GetCountryByCodeQueryValidator() => RuleFor(query => query.CountryCode).MustBeCountryCode();
}
