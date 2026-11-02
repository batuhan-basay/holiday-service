using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Contracts;
using HolidayService.Application.Holidays;
using HolidayService.Application.Holidays.Queries;
using HolidayService.Domain.Common;
using HolidayService.Domain.Holidays;
using HolidayService.UnitTests.TestData;
using NSubstitute;

namespace HolidayService.UnitTests.Application;

public sealed class GetNextHolidaysTests
{
    private readonly IHolidayProvider _provider = Substitute.For<IHolidayProvider>();

    private GetNextHolidays.Handler CreateHandler(DateOnly today) =>
        new(new HolidayRangeLoader(_provider), new FixedDateTimeProvider(today));

    private void GivenYear(int year, params Holiday[] holidays) =>
        _provider
            .GetHolidaysAsync(HolidayFactory.Turkey, year, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<Holiday>>(holidays));

    [Fact]
    public async Task Handle_SkipsHolidaysAlreadyPast()
    {
        GivenYear(
            2026,
            HolidayFactory.On(2026, 1, 1, "New Year"),
            HolidayFactory.On(2026, 4, 23, "April 23"),
            HolidayFactory.On(2026, 5, 1, "Labour Day"));
        GivenYear(2027);
        GivenYear(2028);

        Result<HolidayCollectionResponse> result = await CreateHandler(new DateOnly(2026, 3, 1))
            .Handle(new GetNextHolidays.Query("TR", 5), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Holidays.Select(static holiday => holiday.Name)
            .ShouldBe(new[] { "April 23", "Labour Day" });
    }

    [Fact]
    public async Task Handle_CrossesIntoTheFollowingYear()
    {
        GivenYear(2026, HolidayFactory.On(2026, 12, 31, "Year end"));
        GivenYear(2027, HolidayFactory.On(2027, 1, 1, "New Year"));
        GivenYear(2028);

        Result<HolidayCollectionResponse> result = await CreateHandler(new DateOnly(2026, 12, 30))
            .Handle(new GetNextHolidays.Query("TR", 2), CancellationToken.None);

        result.Value.Holidays.Count.ShouldBe(2);
        result.Value.Holidays[1].Date.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public async Task Handle_HonoursTheRequestedCount()
    {
        GivenYear(
            2026,
            HolidayFactory.On(2026, 6, 1, "A"),
            HolidayFactory.On(2026, 7, 1, "B"),
            HolidayFactory.On(2026, 8, 1, "C"));
        GivenYear(2027);
        GivenYear(2028);

        Result<HolidayCollectionResponse> result = await CreateHandler(new DateOnly(2026, 1, 1))
            .Handle(new GetNextHolidays.Query("TR", 2), CancellationToken.None);

        result.Value.Count.ShouldBe(2);
        result.Value.Holidays.Select(static holiday => holiday.Name).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public async Task Handle_IncludesTodayWhenItIsItselfAHoliday()
    {
        GivenYear(2026, HolidayFactory.On(2026, 5, 19, "Youth Day"));
        GivenYear(2027);
        GivenYear(2028);

        Result<HolidayCollectionResponse> result = await CreateHandler(new DateOnly(2026, 5, 19))
            .Handle(new GetNextHolidays.Query("TR", 1), CancellationToken.None);

        result.Value.Holidays.ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 5, 19));
    }
}
