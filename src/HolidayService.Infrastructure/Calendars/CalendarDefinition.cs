using System.Text.Json.Serialization;

namespace HolidayService.Infrastructure.Calendars;

internal sealed record CalendarDefinition
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("officialName")]
    public string? OfficialName { get; init; }

    [JsonPropertyName("weekend")]
    public IReadOnlyList<string>? Weekend { get; init; }

    [JsonPropertyName("holidays")]
    public IReadOnlyList<HolidayDefinition>? Holidays { get; init; }
}

internal sealed record HolidayDefinition
{
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("localName")]
    public string? LocalName { get; init; }

    [JsonPropertyName("types")]
    public IReadOnlyList<string>? Types { get; init; }

    [JsonPropertyName("substitution")]
    public string? Substitution { get; init; }

    [JsonPropertyName("month")]
    public int? Month { get; init; }

    [JsonPropertyName("day")]
    public int? Day { get; init; }

    [JsonPropertyName("dayOfWeek")]
    public string? DayOfWeek { get; init; }

    [JsonPropertyName("occurrence")]
    public int? Occurrence { get; init; }

    [JsonPropertyName("offsetDays")]
    public int? OffsetDays { get; init; }

    [JsonPropertyName("hijriMonth")]
    public int? HijriMonth { get; init; }

    [JsonPropertyName("hijriDay")]
    public int? HijriDay { get; init; }

    [JsonPropertyName("length")]
    public int? Length { get; init; }

    [JsonPropertyName("dayAdjustment")]
    public int? DayAdjustment { get; init; }
}
