using System.Text.Json.Serialization;

namespace HolidayService.Infrastructure.Nager;

/// <summary>Wire format of <c>GET /api/v3/PublicHolidays/{year}/{countryCode}</c>.</summary>
internal sealed record NagerPublicHoliday
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; init; }

    [JsonPropertyName("localName")]
    public string? LocalName { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("fixed")]
    public bool Fixed { get; init; }

    [JsonPropertyName("global")]
    public bool Global { get; init; }

    [JsonPropertyName("counties")]
    public IReadOnlyList<string>? Counties { get; init; }

    [JsonPropertyName("launchYear")]
    public int? LaunchYear { get; init; }

    [JsonPropertyName("types")]
    public IReadOnlyList<string>? Types { get; init; }
}

/// <summary>Wire format of <c>GET /api/v3/AvailableCountries</c>.</summary>
internal sealed record NagerCountry
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

/// <summary>Wire format of <c>GET /api/v3/CountryInfo/{countryCode}</c>.</summary>
internal sealed record NagerCountryInfo
{
    [JsonPropertyName("commonName")]
    public string? CommonName { get; init; }

    [JsonPropertyName("officialName")]
    public string? OfficialName { get; init; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("borders")]
    public IReadOnlyList<NagerCountryInfo>? Borders { get; init; }
}
