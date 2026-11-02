using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace HolidayService.IntegrationTests.Infrastructure;

internal sealed record HolidayStub(
    string Date,
    string Name,
    string? LocalName = null,
    bool Global = true,
    IReadOnlyList<string>? Counties = null,
    IReadOnlyList<string>? Types = null,
    string? CountryCode = null);

internal sealed record HolidayPayload
{
    [JsonPropertyName("date")]
    public required string Date { get; init; }

    [JsonPropertyName("localName")]
    public required string LocalName { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("countryCode")]
    public required string CountryCode { get; init; }

    [JsonPropertyName("fixed")]
    public bool Fixed { get; init; } = true;

    [JsonPropertyName("global")]
    public bool Global { get; init; } = true;

    [JsonPropertyName("counties")]
    public IReadOnlyList<string>? Counties { get; init; }

    [JsonPropertyName("launchYear")]
    public int? LaunchYear { get; init; }

    [JsonPropertyName("types")]
    public IReadOnlyList<string> Types { get; init; } = ["Public"];
}

internal sealed record CountryPayload
{
    [JsonPropertyName("countryCode")]
    public required string CountryCode { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

internal sealed record CountryInfoPayload
{
    [JsonPropertyName("commonName")]
    public required string CommonName { get; init; }

    [JsonPropertyName("officialName")]
    public required string OfficialName { get; init; }

    [JsonPropertyName("countryCode")]
    public required string CountryCode { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("borders")]
    public IReadOnlyList<CountryInfoPayload>? Borders { get; init; }
}

internal static class NagerStubs
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static WireMockServer StubHolidays(
        this WireMockServer server,
        string countryCode,
        int year,
        params HolidayStub[] holidays)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(holidays);

        List<HolidayPayload> payload =
            [.. holidays.Select(holiday => ToPayload(holiday, countryCode))];

        return server.StubJson(HolidaysPath(countryCode, year), payload);
    }

    public static WireMockServer StubCountries(
        this WireMockServer server,
        params (string Code, string Name)[] countries)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(countries);

        List<CountryPayload> payload =
        [
            .. countries.Select(static country => new CountryPayload
            {
                CountryCode = country.Code,
                Name = country.Name
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
        ArgumentNullException.ThrowIfNull(borders);

        var payload = new CountryInfoPayload
        {
            CommonName = commonName,
            OfficialName = officialName,
            CountryCode = countryCode,
            Region = region,
            Borders =
            [
                .. borders.Select(static border => new CountryInfoPayload
                {
                    CommonName = border.Name,
                    OfficialName = border.Name,
                    CountryCode = border.Code
                })
            ]
        };

        return server.StubJson($"/api/v3/CountryInfo/{countryCode}", payload);
    }

    public static WireMockServer StubWorldwideUpcoming(
        this WireMockServer server,
        params HolidayStub[] holidays)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(holidays);

        List<HolidayPayload> payload =
            [.. holidays.Select(static holiday => ToPayload(holiday, holiday.CountryCode ?? "XX"))];

        return server.StubJson("/api/v3/NextPublicHolidaysWorldwide", payload);
    }

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

    private static WireMockServer StubJson<TPayload>(
        this WireMockServer server,
        string path,
        TPayload payload)
    {
        server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, SerializerOptions)));

        return server;
    }

    private static HolidayPayload ToPayload(HolidayStub holiday, string countryCode) =>
        new()
        {
            Date = holiday.Date,
            LocalName = holiday.LocalName ?? holiday.Name,
            Name = holiday.Name,
            CountryCode = holiday.CountryCode ?? countryCode,
            Global = holiday.Global,
            Counties = holiday.Counties,
            Types = holiday.Types ?? ["Public"]
        };
}
