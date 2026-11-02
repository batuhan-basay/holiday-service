using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Workdays.Queries.ShiftWorkdays;

internal sealed class ShiftWorkdaysQueryValidator : AbstractValidator<ShiftWorkdaysQuery>
{
    public ShiftWorkdaysQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.StartDate.Year).MustBeSupportedYear();
        RuleFor(query => query.Workdays)
            .InclusiveBetween(-ShiftWorkdaysQuery.MaxWorkdays, ShiftWorkdaysQuery.MaxWorkdays)
            .WithMessage($"Workdays must be between -{ShiftWorkdaysQuery.MaxWorkdays} and {ShiftWorkdaysQuery.MaxWorkdays}.");
    }
}
