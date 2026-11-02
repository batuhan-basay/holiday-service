using System.Globalization;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace HolidayService.IntegrationTests.Infrastructure;

/// <summary>Describes one holiday as the upstream API would publish it.</summary>
internal sealed record HolidayStub(
    string Date,
    string Name,
    string? LocalName = null,
    bool Global = true,
    IReadOnlyList<string>? Counties = null,
    IReadOnlyList<string>? Types = null,
    string? CountryCode = null);

/// <summary>Fluent stubs for the upstream Nager.Date endpoints.</summary>
internal static class NagerStubs
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static WireMockServer StubHolidays(
        this WireMockServer server,
        string countryCode,
        int year,
        params HolidayStub[] holidays)
    {
        ArgumentNullException.ThrowIfNull(server);

        string path = string.Create(
            CultureInfo.InvariantCulture,
            $"/api/v3/PublicHolidays/{year}/{countryCode}");

        object[] payload = [.. holidays.Select(holiday => ToPayload(holiday, countryCode))];

        return server.StubJson(path, payload);
    }

    public static WireMockServer StubCountries(
        this WireMockServer server,
        params (string Code, string Name)[] countries)
    {
        ArgumentNullException.ThrowIfNull(server);

        object[] payload =
        [
            .. countries.Select(static country => new
            {
                countryCode = country.Code,
                name = country.Name
            })
        ];

        return server.StubJson("/api/v3/AvailableCountries", payload);
    }

    public static WireMockServer StubCountryInfo(
        this WireMockServer server,
        string countryCode,
        string commonName,
        string officialName,
        string region,
        params (string Code, string Name)[] borders)
    {
        ArgumentNullException.ThrowIfNull(server);

        var payload = new
        {
            commonName,
            officialName,
            countryCode,
            region,
            borders = borders.Select(static border => new
            {
                commonName = border.Name,
                officialName = border.Name,
                countryCode = border.Code,
                region = (string?)null,
                borders = (object?)null
            })
        };

        return server.StubJson($"/api/v3/CountryInfo/{countryCode}", payload);
    }

    public static WireMockServer StubWorldwideUpcoming(
        this WireMockServer server,
        params HolidayStub[] holidays)
    {
        ArgumentNullException.ThrowIfNull(server);

        object[] payload =
        [
            .. holidays.Select(static holiday =>
                ToPayload(holiday, holiday.CountryCode ?? "XX"))
        ];

        return server.StubJson("/api/v3/NextPublicHolidaysWorldwide", payload);
    }

    /// <summary>Makes the upstream answer with a status code instead of data.</summary>
    public static WireMockServer StubStatus(
        this WireMockServer server,
        string path,
        int statusCode)
    {
        ArgumentNullException.ThrowIfNull(server);

        server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode));

        return server;
    }

    public static string HolidaysPath(string countryCode, int year) => string.Create(
        CultureInfo.InvariantCulture,
        $"/api/v3/PublicHolidays/{year}/{countryCode}");

    private static WireMockServer StubJson(this WireMockServer server, string path, object payload)
    {
        server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, SerializerOptions)));

        return server;
    }

    private static object ToPayload(HolidayStub holiday, string countryCode) => new
    {
        date = holiday.Date,
        localName = holiday.LocalName ?? holiday.Name,
        name = holiday.Name,
        countryCode = holiday.CountryCode ?? countryCode,
        @fixed = true,
        global = holiday.Global,
        counties = holiday.Counties,
        launchYear = (int?)null,
        types = holiday.Types ?? ["Public"]
    };
}
