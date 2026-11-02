using HolidayService.Domain.Countries;

namespace HolidayService.Domain.Holidays;

public sealed record Holiday
{
    public required DateOnly Date { get; init; }

    public required string Name { get; init; }

    public required string LocalName { get; init; }

    public required CountryCode CountryCode { get; init; }

    public IReadOnlyList<HolidayType> Types { get; init; } = [];

    public bool IsSubstitute { get; init; }

    public bool IsEstimated { get; init; }

    public DayOfWeek DayOfWeek => Date.DayOfWeek;

    public bool IsPublic => Types.Contains(HolidayType.Public);
}
