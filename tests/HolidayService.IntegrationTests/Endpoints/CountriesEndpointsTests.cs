using System.Net;
using System.Net.Http.Json;
using HolidayService.Application.Contracts;
using HolidayService.IntegrationTests.Infrastructure;

namespace HolidayService.IntegrationTests.Endpoints;

public sealed class CountriesEndpointsTests : IClassFixture<HolidayApiFactory>
{
    private readonly HolidayApiFactory _factory;
    private readonly HttpClient _client;

    public CountriesEndpointsTests(HolidayApiFactory factory)
    {
        _factory = factory;
        _factory.ResetState();
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetCountries_ReturnsTheSupportedCountriesSortedByName()
    {
        _factory.Upstream.StubCountries(("TR", "Turkey"), ("DE", "Germany"), ("AD", "Andorra"));

        IReadOnlyList<CountryResponse>? body = await _client
            .GetFromJsonAsync<IReadOnlyList<CountryResponse>>("/api/v1/countries");

        body.ShouldNotBeNull();
        body!.Select(static country => country.Name)
            .ShouldBe(new[] { "Andorra", "Germany", "Turkey" });
    }

    [Fact]
    public async Task GetCountries_SkipsEntriesWithAnUnusableCode()
    {
        _factory.Upstream.StubCountries(("TR", "Turkey"), ("BAD", "Broken"));

        IReadOnlyList<CountryResponse>? body = await _client
            .GetFromJsonAsync<IReadOnlyList<CountryResponse>>("/api/v1/countries");

        body!.ShouldHaveSingleItem().Code.ShouldBe("TR");
    }

    [Fact]
    public async Task GetCountry_ReturnsTheProfileWithBorders()
    {
        _factory.Upstream.StubCountryInfo(
            "TR",
            "Türkiye",
            "Republic of Türkiye",
            "Asia",
            ("GR", "Greece"),
            ("BG", "Bulgaria"));

        CountryDetailResponse? body = await _client
            .GetFromJsonAsync<CountryDetailResponse>("/api/v1/countries/TR");

        body!.Code.ShouldBe("TR");
        body.CommonName.ShouldBe("Türkiye");
        body.Region.ShouldBe("Asia");
        body.Borders.Select(static border => border.Code).ShouldBe(new[] { "GR", "BG" });
    }

    [Fact]
    public async Task GetCountry_NormalisesALowerCaseCodeBeforeCallingUpstream()
    {
        // Only the canonical upper-case path is stubbed: a 200 proves the domain
        // normalised "tr" to "TR" before the upstream call was made.
        _factory.Upstream.StubCountryInfo("TR", "Türkiye", "Republic of Türkiye", "Asia");

        HttpResponseMessage response = await _client.GetAsync("/api/v1/countries/tr");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        CountryDetailResponse? body =
            await response.Content.ReadFromJsonAsync<CountryDetailResponse>();

        body!.Code.ShouldBe("TR");
    }

    [Fact]
    public async Task GetCountry_UnknownCountryBecomesA404()
    {
        _factory.Upstream.StubStatus("/api/v3/CountryInfo/ZZ", 404);

        HttpResponseMessage response = await _client.GetAsync("/api/v1/countries/ZZ");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        string body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Country.NotSupported");
    }
}
