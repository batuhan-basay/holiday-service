using System.Net.Http.Json;
using HolidayService.Application.Contracts;
using HolidayService.IntegrationTests.Infrastructure;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class WorkdaysEndpointsTests : IClassFixture<HolidayApiFactory>
{
    private readonly HolidayApiFactory _factory;
    private readonly HttpClient _client;

    public WorkdaysEndpointsTests(HolidayApiFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Count_SplitsTheRangeIntoWorkWeekendAndHolidayDays()
    {
        // Monday 2026-03-02 .. Sunday 2026-03-08 with Wednesday off.
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-03-04", "Mid-week"));

        WorkdayCountResponse? body = await _client
            .GetFromJsonAsync<WorkdayCountResponse>(
                "/api/v1/workdays/TR/count?from=2026-03-02&to=2026-03-08");

        body!.TotalDays.ShouldBe(7);
        body.Workdays.ShouldBe(4);
        body.WeekendDays.ShouldBe(2);
        body.PublicHolidayDays.ShouldBe(1);
    }

    [Fact]
    public async Task Shift_SkipsWeekendsAndHolidays()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-03-04", "Mid-week"));

        WorkdayShiftResponse? body = await _client
            .GetFromJsonAsync<WorkdayShiftResponse>(
                "/api/v1/workdays/TR/shift?startDate=2026-03-02&workdays=5");

        body!.ResultDate.ShouldBe(new DateOnly(2026, 3, 10));
        body.ResultDayOfWeek.ShouldBe("Tuesday");
    }

    [Fact]
    public async Task Shift_WalksBackwardsForNegativeValues()
    {
        _factory.Upstream.StubHolidays("TR", 2026);

        WorkdayShiftResponse? body = await _client
            .GetFromJsonAsync<WorkdayShiftResponse>(
                "/api/v1/workdays/TR/shift?startDate=2026-03-02&workdays=-1");

        // Monday minus one workday is the preceding Friday.
        body!.ResultDate.ShouldBe(new DateOnly(2026, 2, 27));
    }

    [Fact]
    public async Task Status_ReportsWhetherADateIsAWorkday()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-03-04", "Mid-week"));

        DayStatusResponse? workday = await _client
            .GetFromJsonAsync<DayStatusResponse>(
                "/api/v1/workdays/TR/status?date=2026-03-03");

        DayStatusResponse? holiday = await _client
            .GetFromJsonAsync<DayStatusResponse>(
                "/api/v1/workdays/TR/status?date=2026-03-04");

        workday!.IsWorkday.ShouldBeTrue();
        holiday!.IsWorkday.ShouldBeFalse();
        holiday.IsPublicHoliday.ShouldBeTrue();
    }
}
