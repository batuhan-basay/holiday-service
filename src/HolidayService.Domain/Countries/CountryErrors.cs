using HolidayService.Domain.Common;

namespace HolidayService.Domain.Countries;

public static class CountryErrors
{
    public static readonly Error CodeRequired = Error.Validation(
        "Country.CodeRequired",
        "A country code is required.");

    public static Error CodeInvalid(string value) => Error.Validation(
        "Country.CodeInvalid",
        $"'{value}' is not a valid ISO 3166-1 alpha-2 country code.");

    public static Error NotSupported(string code) => Error.NotFound(
        "Country.NotSupported",
        $"Country '{code}' is not covered by this service.");
}
