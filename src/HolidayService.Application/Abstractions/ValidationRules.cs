using FluentValidation;

namespace HolidayService.Application.Abstractions;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string> MustBeCountryCode<T>(
        this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .WithMessage("Country code is required.")
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Country code must be an ISO 3166-1 alpha-2 value, for example 'TR'.");

    public static IRuleBuilderOptions<T, int> MustBeSupportedYear<T>(
        this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(SupportedYears.Min, SupportedYears.Max)
            .WithMessage($"Year must be between {SupportedYears.Min} and {SupportedYears.Max}.");
}
