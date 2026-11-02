using HolidayService.Domain.Countries;

namespace HolidayService.Domain.Holidays;

/// <summary>A single public holiday occurrence on a concrete date.</summary>
public sealed record Holiday
{
    public required DateOnly Date { get; init; }

    /// <summary>English name, e.g. "New Year's Day".</summary>
    public required string Name { get; init; }

    /// <summary>Name in the country's own language, e.g. "Yılbaşı".</summary>
    public required string LocalName { get; init; }

    public required CountryCode CountryCode { get; init; }

    /// <summary>True when the holiday always falls on the same calendar day.</summary>
    public bool IsFixed { get; init; }

    /// <summary>False when the holiday only applies to the subdivisions in <see cref="Counties"/>.</summary>
    public bool IsGlobal { get; init; } = true;

    /// <summary>ISO 3166-2 subdivision codes the holiday is limited to.</summary>
    public IReadOnlyList<string> Counties { get; init; } = [];

    public int? LaunchYear { get; init; }

    public IReadOnlyList<HolidayType> Types { get; init; } = [];

    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public bool IsPublic => Types.Contains(HolidayType.Public);

    /// <summary>True when this holiday applies in the given subdivision (or nationwide).</summary>
    public bool AppliesTo(string? county) =>
        IsGlobal || county is null || Counties.Contains(county, StringComparer.OrdinalIgnoreCase);
}
