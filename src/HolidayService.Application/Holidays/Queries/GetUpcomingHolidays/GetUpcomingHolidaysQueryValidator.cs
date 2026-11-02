using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetUpcomingHolidays;

internal sealed class GetUpcomingHolidaysQueryValidator : AbstractValidator<GetUpcomingHolidaysQuery>
{
    public GetUpcomingHolidaysQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Days)
            .InclusiveBetween(1, GetUpcomingHolidaysQuery.MaxDays)
            .WithMessage($"Days must be between 1 and {GetUpcomingHolidaysQuery.MaxDays}.");
    }
}
