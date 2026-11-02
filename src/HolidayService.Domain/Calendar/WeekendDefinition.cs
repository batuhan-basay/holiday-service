using HolidayService.Domain.Countries;

namespace HolidayService.Domain.Calendar;

/// <summary>
/// Which weekdays count as the weekend. Not every country rests on Saturday and Sunday,
/// so business-day maths has to be parameterised rather than hard-coded.
/// </summary>
public sealed class WeekendDefinition
{
    private readonly HashSet<DayOfWeek> _days;

    private WeekendDefinition(params DayOfWeek[] days) => _days = [.. days];

    /// <summary>Saturday and Sunday.</summary>
    public static WeekendDefinition SaturdaySunday { get; } = new(DayOfWeek.Saturday, DayOfWeek.Sunday);

    /// <summary>Friday and Saturday — common across much of the Middle East.</summary>
    public static WeekendDefinition FridaySaturday { get; } = new(DayOfWeek.Friday, DayOfWeek.Saturday);

    /// <summary>Sunday only — e.g. India, Nepal.</summary>
    public static WeekendDefinition SundayOnly { get; } = new(DayOfWeek.Sunday);

    /// <summary>Friday and Sunday — Brunei.</summary>
    public static WeekendDefinition FridaySunday { get; } = new(DayOfWeek.Friday, DayOfWeek.Sunday);

    public IReadOnlySet<DayOfWeek> Days => _days;

    public bool Includes(DayOfWeek day) => _days.Contains(day);

    public bool Includes(DateOnly date) => _days.Contains(date.DayOfWeek);

    private static readonly Dictionary<string, WeekendDefinition> Overrides = new(StringComparer.Ordinal)
    {
        ["AE"] = SaturdaySunday,
        ["AF"] = new(DayOfWeek.Thursday, DayOfWeek.Friday),
        ["BD"] = new(DayOfWeek.Friday, DayOfWeek.Saturday),
        ["BN"] = FridaySunday,
        ["DJ"] = new(DayOfWeek.Friday),
        ["DZ"] = FridaySaturday,
        ["EG"] = FridaySaturday,
        ["IL"] = FridaySaturday,
        ["IN"] = SundayOnly,
        ["IQ"] = FridaySaturday,
        ["IR"] = new(DayOfWeek.Friday),
        ["JO"] = FridaySaturday,
        ["KW"] = FridaySaturday,
        ["LY"] = FridaySaturday,
        ["MV"] = FridaySaturday,
        ["NP"] = new(DayOfWeek.Saturday),
        ["OM"] = FridaySaturday,
        ["PS"] = FridaySaturday,
        ["QA"] = FridaySaturday,
        ["SA"] = FridaySaturday,
        ["SD"] = FridaySaturday,
        ["SY"] = FridaySaturday,
        ["YE"] = FridaySaturday
    };

    /// <summary>Resolves the weekend for a country, defaulting to Saturday/Sunday.</summary>
    public static WeekendDefinition ForCountry(CountryCode code) =>
        Overrides.TryGetValue(code.Value, out WeekendDefinition? weekend) ? weekend : SaturdaySunday;

    public static WeekendDefinition Custom(params DayOfWeek[] days)
    {
        ArgumentNullException.ThrowIfNull(days);

        if (days.Length == 0)
        {
            throw new ArgumentException("A weekend definition needs at least one day.", nameof(days));
        }

        if (days.Distinct().Count() == 7)
        {
            throw new ArgumentException("A weekend cannot span every day of the week.", nameof(days));
        }

        return new WeekendDefinition(days);
    }
}
