using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetNextHolidays;

internal sealed class GetNextHolidaysQueryValidator : AbstractValidator<GetNextHolidaysQuery>
{
    public GetNextHolidaysQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Count)
            .InclusiveBetween(1, GetNextHolidaysQuery.MaxCount)
            .WithMessage($"Count must be between 1 and {GetNextHolidaysQuery.MaxCount}.");
    }
}
