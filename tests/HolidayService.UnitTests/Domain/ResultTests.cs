using HolidayService.Domain.Common;

namespace HolidayService.UnitTests.Domain;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.NotFound("Test.Missing", "Not here.");

    [Fact]
    public void Success_ExposesValue()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_ReadingValueThrows()
    {
        Result<int> result = Result.Failure<int>(SampleError);

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => { _ = result.Value; });
    }

    [Fact]
    public void Map_ProjectsSuccessAndPropagatesFailure()
    {
        Result.Success(2).Map(static value => value * 10).Value.ShouldBe(20);

        Result<int> failed = Result.Failure<int>(SampleError);

        failed.Map(static value => value * 10).Error.ShouldBe(SampleError);
    }

    [Fact]
    public void Bind_ChainsOnlyOnSuccess()
    {
        Result<string> chained = Result
            .Success(5)
            .Bind(static value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        chained.Value.ShouldBe("5");

        Result<string> shortCircuited = Result
            .Failure<int>(SampleError)
            .Bind(static value => Result.Success(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        shortCircuited.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Match_SelectsTheMatchingBranch()
    {
        Result.Success(1).Match(static _ => "ok", static _ => "fail").ShouldBe("ok");
        Result.Failure<int>(SampleError).Match(static _ => "ok", static _ => "fail").ShouldBe("fail");
    }

    [Fact]
    public void ImplicitConversion_WrapsValueAsSuccess()
    {
        Result<string> result = "value";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("value");
    }
}
