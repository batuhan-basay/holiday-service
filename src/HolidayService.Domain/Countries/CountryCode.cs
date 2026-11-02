using System.Diagnostics.CodeAnalysis;
using HolidayService.Domain.Common;

namespace HolidayService.Domain.Countries;

public readonly record struct CountryCode
{
    private CountryCode(string value) => Value = value;

    public string Value { get; }

    public static Result<CountryCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CountryCode>(CountryErrors.CodeRequired);
        }

        string trimmed = value.Trim();

        if (trimmed.Length != 2 || !char.IsAsciiLetter(trimmed[0]) || !char.IsAsciiLetter(trimmed[1]))
        {
            return Result.Failure<CountryCode>(CountryErrors.CodeInvalid(trimmed));
        }

        return Result.Success(new CountryCode(trimmed.ToUpperInvariant()));
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out CountryCode? code)
    {
        Result<CountryCode> result = Create(value);
        code = result.IsSuccess ? result.Value : null;
        return result.IsSuccess;
    }

    public override string ToString() => Value;

    public static implicit operator string(CountryCode code) => code.Value;
}
