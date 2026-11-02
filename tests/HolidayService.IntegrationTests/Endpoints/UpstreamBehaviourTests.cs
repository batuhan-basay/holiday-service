using System.Net;
using System.Net.Http.Json;
using HolidayService.Application.Contracts;
using HolidayService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class UpstreamBehaviourTests : IClassFixture<HolidayApiFactory>
{
    private readonly HolidayApiFactory _factory;
    private readonly HttpClient _client;

    public UpstreamBehaviourTests(HolidayApiFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task UpstreamServerError_BecomesServiceUnavailable()
    {
        _factory.Upstream.StubStatus(NagerStubs.HolidaysPath("TR", 2026), 500);

        HttpResponseMessage response = await _client.GetAsync("/api/v1/holidays/TR/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        problem.ShouldNotBeNull();
        problem!.Title.ShouldBe("Upstream provider unavailable");
        problem.Extensions["errorCode"]?.ToString()
            .ShouldBe("Holiday.ProviderUnavailable");
    }

    [Fact]
    public async Task UpstreamNotFound_BecomesNotFound()
    {
        _factory.Upstream.StubStatus(NagerStubs.HolidaysPath("ZZ", 2026), 404);

        HttpResponseMessage response = await _client.GetAsync("/api/v1/holidays/ZZ/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RepeatedRequests_HitTheUpstreamOnlyOnce()
    {
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-01-01", "New Year"));

        await _client.GetAsync("/api/v1/holidays/TR/2026");
        await _client.GetAsync("/api/v1/holidays/TR/2026");
        await _client.GetAsync("/api/v1/holidays/TR/2026/1");

        int upstreamCalls = _factory.Upstream.LogEntries.Count(entry =>
            string.Equals(
                entry.RequestMessage.Path,
                NagerStubs.HolidaysPath("TR", 2026),
                StringComparison.Ordinal));

        upstreamCalls.ShouldBe(1);
    }

    [Fact]
    public async Task AFailedUpstreamCallIsNotCached()
    {
        _factory.Upstream.StubStatus(NagerStubs.HolidaysPath("TR", 2026), 500);

        await _client.GetAsync("/api/v1/holidays/TR/2026");
        await _client.GetAsync("/api/v1/holidays/TR/2026");

        int upstreamCalls = _factory.Upstream.LogEntries.Count(entry =>
            string.Equals(
                entry.RequestMessage.Path,
                NagerStubs.HolidaysPath("TR", 2026),
                StringComparison.Ordinal));

        upstreamCalls.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task EmptyUpstreamPayload_IsAnEmptyCollectionNotAnError()
    {
        _factory.Upstream.StubHolidays("TR", 2026);

        HolidayCollectionResponse? body = await _client
            .GetFromJsonAsync<HolidayCollectionResponse>("/api/v1/holidays/TR/2026");

        body!.Count.ShouldBe(0);
        body.Holidays.ShouldBeEmpty();
    }
}
