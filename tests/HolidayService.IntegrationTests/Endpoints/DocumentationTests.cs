using System.Net;
using HolidayService.IntegrationTests.Infrastructure;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class DocumentationTests(HolidayApiFactory factory)
    : IClassFixture<HolidayApiFactory>
{
    [Fact]
    public async Task SwaggerDocument_IsGeneratedForV1()
    {
        HttpResponseMessage response = await factory
            .CreateClient()
            .GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        string document = await response.Content.ReadAsStringAsync();

        document.ShouldContain("/api/v1/holidays/{countryCode}/{year}");
        document.ShouldContain("X-API-Key");
    }

    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await client.GetAsync("/");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldBe("/swagger");
    }
}
