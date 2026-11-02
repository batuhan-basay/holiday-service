using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Workdays.Queries.CountWorkdays;

internal sealed class CountWorkdaysQueryValidator : AbstractValidator<CountWorkdaysQuery>
{
    public CountWorkdaysQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.From.Year).MustBeSupportedYear();
        RuleFor(query => query.To.Year).MustBeSupportedYear();

        RuleFor(query => query)
            .Must(query => query.From <= query.To)
            .WithName("to")
            .WithMessage("'from' must be earlier than or equal to 'to'.");

        RuleFor(query => query)
            .Must(query => query.To.DayNumber - query.From.DayNumber + 1 <= CountWorkdaysQuery.MaxRangeDays)
            .WithName("to")
            .WithMessage($"The requested range may not exceed {CountWorkdaysQuery.MaxRangeDays} days.");
    }
}
