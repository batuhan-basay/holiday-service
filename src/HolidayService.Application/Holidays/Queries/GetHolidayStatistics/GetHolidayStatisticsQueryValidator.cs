using FluentValidation;
using HolidayService.Application.Abstractions;

namespace HolidayService.Application.Holidays.Queries.GetHolidayStatistics;

internal sealed class GetHolidayStatisticsQueryValidator : AbstractValidator<GetHolidayStatisticsQuery>
{
    public GetHolidayStatisticsQueryValidator()
    {
        RuleFor(query => query.CountryCode).MustBeCountryCode();
        RuleFor(query => query.Year).MustBeSupportedYear();
    }
}
