using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByYear;

internal sealed class GetHolidaysByYearQueryValidator : AbstractValidator<GetHolidaysByYearQuery>
{
    public GetHolidaysByYearQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Year).MustBeSupportedYear();
    }
}
