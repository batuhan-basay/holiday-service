using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByMonth;

internal sealed class GetHolidaysByMonthQueryValidator : AbstractValidator<GetHolidaysByMonthQuery>
{
    public GetHolidaysByMonthQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Year).MustBeSupportedYear();
        RuleFor(query => query.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");
    }
}
