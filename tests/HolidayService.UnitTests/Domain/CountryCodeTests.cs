using HolidayService.Domain.Countries;

namespace HolidayService.UnitTests.Domain;

public sealed class CountryCodeTests
{
    [Theory]
    [InlineData("tr", "TR")]
    [InlineData("TR", "TR")]
    [InlineData(" de ", "DE")]
    public void Create_NormalisesToUpperCase(string input, string expected)
    {
        var result = CountryCode.Create(input);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_RejectsMissingCode(string? input)
    {
        var result = CountryCode.Create(input);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(CountryErrors.CodeRequired);
    }

    [Theory]
    [InlineData("T")]
    [InlineData("TUR")]
    [InlineData("T1")]
    [InlineData("12")]
    public void Create_RejectsNonAlpha2Values(string input)
    {
        var result = CountryCode.Create(input);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Country.CodeInvalid");
    }

    [Fact]
    public void Equality_IsCaseInsensitiveThroughNormalisation()
    {
        CountryCode lower = CountryCode.Create("tr").Value;
        CountryCode upper = CountryCode.Create("TR").Value;

        lower.ShouldBe(upper);
    }

    [Fact]
    public void TryCreate_ReturnsFalseAndNullForInvalidInput()
    {
        bool created = CountryCode.TryCreate("nope", out CountryCode? code);

        created.ShouldBeFalse();
        code.ShouldBeNull();
    }
}
