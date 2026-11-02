using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Domain.Common;
using HolidayService.Domain.Holidays;
using HolidayService.UnitTests.TestData;
using NSubstitute;

namespace HolidayService.UnitTests.Application;

public sealed class GetDayStatusTests
{
    private readonly IHolidayProvider _provider = Substitute.For<IHolidayProvider>();

    private GetDayStatus.Handler CreateHandler() => new(_provider);

    private void GivenHolidays(params Holiday[] holidays) =>
        _provider
            .GetHolidaysAsync(HolidayFactory.Turkey, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<Holiday>>(holidays));

    [Fact]
    public async Task Handle_ReportsAPublicHoliday()
    {
        GivenHolidays(HolidayFactory.On(2026, 4, 23, "National Sovereignty Day"));

        Result<DayStatusResponse> result = await CreateHandler().Handle(
            new GetDayStatus.Query("TR", new DateOnly(2026, 4, 23)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsPublicHoliday.ShouldBeTrue();
        result.Value.IsWorkday.ShouldBeFalse();
        result.Value.Holiday.ShouldNotBeNull();
        result.Value.Holiday!.Name.ShouldBe("National Sovereignty Day");
    }

    [Fact]
    public async Task Handle_ReportsAnOrdinaryWorkday()
    {
        GivenHolidays();

        Result<DayStatusResponse> result = await CreateHandler().Handle(
            new GetDayStatus.Query("TR", new DateOnly(2026, 1, 5)),
            CancellationToken.None);

        result.Value.IsWorkday.ShouldBeTrue();
        result.Value.IsWeekend.ShouldBeFalse();
        result.Value.IsPublicHoliday.ShouldBeFalse();
        result.Value.Holiday.ShouldBeNull();
        result.Value.DayOfWeek.ShouldBe("Monday");
    }

    [Fact]
    public async Task Handle_ReportsAWeekendDay()
    {
        GivenHolidays();

        Result<DayStatusResponse> result = await CreateHandler().Handle(
            new GetDayStatus.Query("TR", new DateOnly(2026, 1, 3)),
            CancellationToken.None);

        result.Value.IsWeekend.ShouldBeTrue();
        result.Value.IsWorkday.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_PropagatesAnInvalidCountryCodeAsValidationError()
    {
        Result<DayStatusResponse> result = await CreateHandler().Handle(
            new GetDayStatus.Query("TUR", new DateOnly(2026, 1, 5)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_PropagatesProviderFailure()
    {
        _provider
            .GetHolidaysAsync(HolidayFactory.Turkey, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<Holiday>>(HolidayErrors.ProviderUnavailable));

        Result<DayStatusResponse> result = await CreateHandler().Handle(
            new GetDayStatus.Query("TR", new DateOnly(2026, 1, 5)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
    }
}
