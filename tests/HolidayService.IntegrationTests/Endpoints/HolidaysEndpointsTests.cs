using System.Net;
using System.Net.Http.Json;
using HolidayService.Application.Contracts;
using HolidayService.IntegrationTests.Infrastructure;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class HolidaysEndpointsTests : IClassFixture<HolidayApiFactory>
{
    private readonly HolidayApiFactory _factory;
    private readonly HttpClient _client;

    public HolidaysEndpointsTests(HolidayApiFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetByYear_ReturnsTheHolidaysOfThatYear()
    {
        _factory.Upstream.StubHolidays(
            "TR",
            2026,
            new HolidayStub("2026-01-01", "New Year's Day", "Yılbaşı"),
            new HolidayStub("2026-04-23", "National Sovereignty Day"));

        HttpResponseMessage response = await _client.GetAsync("/api/v1/holidays/TR/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        HolidayCollectionResponse? body =
            await response.Content.ReadFromJsonAsync<HolidayCollectionResponse>();

        body.ShouldNotBeNull();
        body!.CountryCode.ShouldBe("TR");
        body.Count.ShouldBe(2);
        body.Holidays[0].Date.ShouldBe(new DateOnly(2026, 1, 1));
        body.Holidays[0].LocalName.ShouldBe("Yılbaşı");
        body.Holidays[0].DayOfWeek.ShouldBe("Thursday");
    }

    [Fact]
    public async Task GetByYear_FiltersByCounty()
    {
        _factory.Upstream.StubHolidays(
            "DE",
            2026,
            new HolidayStub("2026-01-01", "Neujahr"),
            new HolidayStub(
                "2026-11-01",
                "Allerheiligen",
                Global: false,
                Counties: ["DE-BY", "DE-NW"]));

        HolidayCollectionResponse? bavaria = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>(
                "/api/v1/holidays/DE/2026?county=DE-BY");

        HolidayCollectionResponse? hesse = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>(
                "/api/v1/holidays/DE/2026?county=DE-HE");

        bavaria!.Count.ShouldBe(2);
        hesse!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByMonth_NarrowsToASingleMonth()
    {
        _factory.Upstream.StubHolidays(
            "TR",
            2026,
            new HolidayStub("2026-03-15", "March holiday"),
            new HolidayStub("2026-05-01", "Labour Day"));

        HolidayCollectionResponse? body = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>("/api/v1/holidays/TR/2026/3");

        body!.Count.ShouldBe(1);
        body.From.ShouldBe(new DateOnly(2026, 3, 1));
        body.To.ShouldBe(new DateOnly(2026, 3, 31));
        body.Holidays.ShouldHaveSingleItem().Name.ShouldBe("March holiday");
    }

    [Fact]
    public async Task GetByRange_SpansTheYearBoundary()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-12-31", "Year end"));
        _factory.Upstream.StubHolidays("TR", 2027, new HolidayStub("2027-01-01", "New Year"));

        HolidayCollectionResponse? body = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>(
                "/api/v1/holidays/TR/range?from=2026-12-28&to=2027-01-03");

        body!.Count.ShouldBe(2);
        body.Holidays[0].Date.ShouldBe(new DateOnly(2026, 12, 31));
        body.Holidays[1].Date.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public async Task GetNext_ReturnsUpcomingHolidaysRelativeToToday()
    {
        _factory.Upstream.StubHolidays(
            "TR",
            2026,
            new HolidayStub("2026-01-01", "Already past"),
            new HolidayStub("2026-03-15", "Soon"),
            new HolidayStub("2026-05-01", "Later"));
        _factory.Upstream.StubHolidays("TR", 2027);
        _factory.Upstream.StubHolidays("TR", 2028);

        HolidayCollectionResponse? body = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>("/api/v1/holidays/TR/next?count=2");

        body!.Count.ShouldBe(2);
        body.Holidays.Select(static holiday => holiday.Name)
            .ShouldBe(new[] { "Soon", "Later" });
    }

    [Fact]
    public async Task GetUpcoming_HonoursTheDayWindow()
    {
        _factory.Upstream.StubHolidays(
            "TR",
            2026,
            new HolidayStub("2026-03-15", "Inside the window"),
            new HolidayStub("2026-05-01", "Outside the window"));

        HolidayCollectionResponse? body = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>("/api/v1/holidays/TR/upcoming?days=30");

        body!.Holidays.ShouldHaveSingleItem().Name.ShouldBe("Inside the window");
    }

    [Fact]
    public async Task GetLongWeekends_DetectsAHolidayNextToTheWeekend()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-01-02", "Bridge"));

        IReadOnlyList<LongWeekendResponse>? body = await _client
            .GetFromJsonAsync<IReadOnlyList<LongWeekendResponse>>(
                "/api/v1/holidays/TR/long-weekends/2026?includeBridgeDays=false");

        body.ShouldNotBeNull();
        body!.ShouldHaveSingleItem();
        body[0].StartDate.ShouldBe(new DateOnly(2026, 1, 2));
        body[0].EndDate.ShouldBe(new DateOnly(2026, 1, 4));
        body[0].DayCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetStatistics_SeparatesWeekendOverlap()
    {
        _factory.Upstream.StubHolidays(
            "TR",
            2026,
            new HolidayStub("2026-01-01", "Thursday holiday"),
            new HolidayStub("2026-01-03", "Saturday holiday"),
            new HolidayStub("2026-05-01", "Friday holiday"));

        HolidayStatisticsResponse? body = await _client
            .GetFromJsonAsync<HolidayStatisticsResponse>("/api/v1/holidays/TR/statistics/2026");

        body!.TotalHolidays.ShouldBe(3);
        body.FallingOnWeekend.ShouldBe(1);
        body.EffectiveDaysOff.ShouldBe(2);
        body.ByMonth.Count.ShouldBe(12);
        body.ByDayOfWeek.Count.ShouldBe(7);
    }

    [Fact]
    public async Task CheckDate_ReportsHolidayWeekendAndWorkday()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-04-23", "April 23"));

        DayStatusResponse? holiday = await _client
            .GetFromJsonAsync<DayStatusResponse>("/api/v1/holidays/TR/check?date=2026-04-23");

        DayStatusResponse? weekend = await _client
            .GetFromJsonAsync<DayStatusResponse>("/api/v1/holidays/TR/check?date=2026-04-25");

        holiday!.IsPublicHoliday.ShouldBeTrue();
        holiday.IsWorkday.ShouldBeFalse();
        holiday.Holiday.ShouldNotBeNull();

        weekend!.IsWeekend.ShouldBeTrue();
        weekend.IsPublicHoliday.ShouldBeFalse();
        weekend.Holiday.ShouldBeNull();
    }

    [Fact]
    public async Task GetWorldwideUpcoming_ReturnsHolidaysAcrossCountries()
    {
        _factory.Upstream.StubWorldwideUpcoming(
            new HolidayStub("2026-03-15", "Holiday A", CountryCode: "TR"),
            new HolidayStub("2026-03-16", "Holiday B", CountryCode: "DE"));

        IReadOnlyList<HolidayResponse>? body = await _client
            .GetFromJsonAsync<IReadOnlyList<HolidayResponse>>(
                "/api/v1/holidays/worldwide/upcoming");

        body!.Count.ShouldBe(2);
        body.Select(static holiday => holiday.CountryCode).ShouldBe(new[] { "TR", "DE" });
    }
}
