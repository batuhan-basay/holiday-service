using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetDayStatus;

internal sealed class GetDayStatusQueryValidator : AbstractValidator<GetDayStatusQuery>
{
    public GetDayStatusQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Date.Year).MustBeSupportedYear();
    }
}
