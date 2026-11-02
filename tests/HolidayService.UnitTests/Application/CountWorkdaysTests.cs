using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Application.Workdays.Queries;
using HolidayService.Domain.Common;
using HolidayService.Domain.Holidays;
using HolidayService.UnitTests.TestData;
using NSubstitute;

namespace HolidayService.UnitTests.Application;

public sealed class CountWorkdaysTests
{
    private readonly IHolidayProvider _provider = Substitute.For<IHolidayProvider>();

    private CountWorkdays.Handler CreateHandler() => new(new HolidayRangeLoader(_provider));

    private void GivenYear(int year, params Holiday[] holidays) =>
        _provider
            .GetHolidaysAsync(HolidayFactory.Turkey, year, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<Holiday>>(holidays));

    [Fact]
    public async Task Handle_SplitsTheRangeIntoWorkWeekendAndHolidayDays()
    {
        GivenYear(2026, HolidayFactory.On(2026, 1, 7, "Mid-week"));

        Result<WorkdayCountResponse> result = await CreateHandler().Handle(
            new CountWorkdays.Query("TR", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalDays.ShouldBe(7);
        result.Value.Workdays.ShouldBe(4);
        result.Value.WeekendDays.ShouldBe(2);
        result.Value.PublicHolidayDays.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_RejectsAnInvertedRange()
    {
        GivenYear(2026);

        Result<WorkdayCountResponse> result = await CreateHandler().Handle(
            new CountWorkdays.Query("TR", new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Holiday.RangeInverted");
    }

    [Fact]
    public async Task Handle_RejectsARangeWiderThanTheLimit()
    {
        GivenYear(2026);

        Result<WorkdayCountResponse> result = await CreateHandler().Handle(
            new CountWorkdays.Query(
                "TR",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1).AddDays(CountWorkdays.MaxRangeDays)),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Holiday.RangeTooWide");
    }

    [Fact]
    public async Task Handle_CountsASingleDayRange()
    {
        GivenYear(2026);

        Result<WorkdayCountResponse> result = await CreateHandler().Handle(
            new CountWorkdays.Query("TR", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5)),
            CancellationToken.None);

        result.Value.TotalDays.ShouldBe(1);
        result.Value.Workdays.ShouldBe(1);
    }
}
