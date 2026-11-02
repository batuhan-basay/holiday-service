using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetLongWeekends;

internal sealed class GetLongWeekendsQueryValidator : AbstractValidator<GetLongWeekendsQuery>
{
    public GetLongWeekendsQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Year).MustBeSupportedYear();
    }
}
