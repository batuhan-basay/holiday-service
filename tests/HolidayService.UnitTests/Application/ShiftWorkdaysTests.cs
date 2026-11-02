using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Application.Workdays.Queries.ShiftWorkdays;
using HolidayService.Domain.Common;
using HolidayService.Domain.Holidays;
using HolidayService.UnitTests.TestData;
using NSubstitute;

namespace HolidayService.UnitTests.Application;

public sealed class ShiftWorkdaysTests
{
    private readonly IHolidayProvider _provider = Substitute.For<IHolidayProvider>();

    public ShiftWorkdaysTests() =>
        _provider
            .GetHolidaysAsync(HolidayFactory.Turkey, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<Holiday>>(
                [HolidayFactory.On(2026, 1, 6, "Tuesday off")]));

    private ShiftWorkdaysQueryHandler CreateHandler() => new(new HolidayRangeLoader(_provider));

    [Fact]
    public async Task Handle_SkipsWeekendsAndHolidaysGoingForward()
    {
        Result<WorkdayShiftResponse> result = await CreateHandler().Handle(
            new ShiftWorkdaysQuery("TR", new DateOnly(2026, 1, 2), 2),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ResultDate.ShouldBe(new DateOnly(2026, 1, 7));
        result.Value.ResultDayOfWeek.ShouldBe("Wednesday");
    }

    [Fact]
    public async Task Handle_WalksBackwardsForNegativeValues()
    {
        Result<WorkdayShiftResponse> result = await CreateHandler().Handle(
            new ShiftWorkdaysQuery("TR", new DateOnly(2026, 1, 7), -2),
            CancellationToken.None);

        result.Value.ResultDate.ShouldBe(new DateOnly(2026, 1, 2));
    }

    [Fact]
    public async Task Handle_ReturnsTheStartDateForZero()
    {
        Result<WorkdayShiftResponse> result = await CreateHandler().Handle(
            new ShiftWorkdaysQuery("TR", new DateOnly(2026, 1, 3), 0),
            CancellationToken.None);

        result.Value.ResultDate.ShouldBe(new DateOnly(2026, 1, 3));
    }
}
