using System.Net;
using System.Net.Http.Json;
using HolidayService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class SecurityAndValidationTests : IClassFixture<HolidayApiFactory>
{
    private readonly HolidayApiFactory _factory;

    public SecurityAndValidationTests(HolidayApiFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _factory.Upstream.StubHolidays("TR", 2026, new HolidayStub("2026-01-01", "New Year"));
    }

    [Fact]
    public async Task RequestWithoutAnApiKey_IsRejected()
    {
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/v1/holidays/TR/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWithAWrongApiKey_IsRejected()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "not-the-right-key");

        HttpResponseMessage response = await client.GetAsync("/api/v1/holidays/TR/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWithAValidApiKey_IsAccepted()
    {
        HttpResponseMessage response = await _factory
            .CreateAuthenticatedClient()
            .GetAsync("/api/v1/holidays/TR/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_NeedsNoApiKey()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvalidCountryCode_ReturnsValidationProblemDetails()
    {
        HttpResponseMessage response = await _factory
            .CreateAuthenticatedClient()
            .GetAsync("/api/v1/holidays/TURKEY/2026");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        problem.ShouldNotBeNull();
        problem!.Errors.Keys.ShouldContain("CountryCode");
    }

    [Fact]
    public async Task YearOutsideTheSupportedWindow_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _factory
            .CreateAuthenticatedClient()
            .GetAsync("/api/v1/holidays/TR/1800");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        problem!.Errors.Keys.ShouldContain("Year");
    }

    [Fact]
    public async Task InvertedDateRange_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _factory
            .CreateAuthenticatedClient()
            .GetAsync("/api/v1/holidays/TR/range?from=2026-06-01&to=2026-01-01");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnparseableDate_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _factory
            .CreateAuthenticatedClient()
            .GetAsync("/api/v1/holidays/TR/check?date=not-a-date");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
