using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetHolidaysByRange;

internal sealed class GetHolidaysByRangeQueryValidator : AbstractValidator<GetHolidaysByRangeQuery>
{
    public GetHolidaysByRangeQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();

        RuleFor(query => query.From.Year).MustBeSupportedYear();
        RuleFor(query => query.To.Year).MustBeSupportedYear();

        RuleFor(query => query)
            .Must(query => query.From <= query.To)
            .WithName("to")
            .WithMessage("'from' must be earlier than or equal to 'to'.");

        RuleFor(query => query)
            .Must(query => query.To.DayNumber - query.From.DayNumber + 1 <= GetHolidaysByRangeQuery.MaxRangeDays)
            .WithName("to")
            .WithMessage($"The requested range may not exceed {GetHolidaysByRangeQuery.MaxRangeDays} days.");
    }
}
