using HolidayService.Domain.Countries;

namespace HolidayService.Domain.Holidays;

public sealed record Holiday
{
    public required DateOnly Date { get; init; }

    public required string Name { get; init; }

    public required string LocalName { get; init; }

    public required CountryCode CountryCode { get; init; }

    public bool IsFixed { get; init; }

    public bool IsGlobal { get; init; } = true;

    public IReadOnlyList<string> Counties { get; init; } = [];

    public int? LaunchYear { get; init; }

    public IReadOnlyList<HolidayType> Types { get; init; } = [];

    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public bool IsPublic => Types.Contains(HolidayType.Public);

    public bool AppliesTo(string? county) =>
        IsGlobal || county is null || Counties.Contains(county, StringComparer.OrdinalIgnoreCase);
}
