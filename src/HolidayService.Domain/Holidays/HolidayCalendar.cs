using HolidayService.Domain.Calendar;
using HolidayService.Domain.Countries;

namespace HolidayService.Domain.Holidays;

public sealed class HolidayCalendar
{
    public HolidayCalendar(
        CountryCode code,
        string name,
        string officialName,
        WeekendDefinition weekend,
        IReadOnlyList<HolidayRule> rules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(weekend);
        ArgumentNullException.ThrowIfNull(rules);

        Code = code;
        Name = name;
        OfficialName = string.IsNullOrWhiteSpace(officialName) ? name : officialName;
        Weekend = weekend;
        Rules = rules;
    }

    public CountryCode Code { get; }

    public string Name { get; }

    public string OfficialName { get; }

    public WeekendDefinition Weekend { get; }

    public IReadOnlyList<HolidayRule> Rules { get; }

    public CountrySummary ToSummary() => new(Code, Name);

    public Country ToCountry() => new(Code, Name, OfficialName, [.. Weekend.Days]);

    public IReadOnlyList<Holiday> ForYear(int year)
    {
        var direct = new List<(HolidayRule Rule, DateOnly Date)>();
        var deferred = new List<(HolidayRule Rule, DateOnly Date)>();

        foreach (HolidayRule rule in Rules)
        {
            foreach (DateOnly date in rule.OccurrencesIn(year))
            {
                if (rule.Substitution != SubstitutionPolicy.None && Weekend.Includes(date))
                {
                    deferred.Add((rule, date));
                }
                else
                {
                    direct.Add((rule, date));
                }
            }
        }

        var occupied = direct.Select(static entry => entry.Date).ToHashSet();
        var holidays = new List<Holiday>(direct.Count + deferred.Count);

        foreach ((HolidayRule rule, DateOnly date) in direct)
        {
            holidays.Add(Create(rule, date, isSubstitute: false));
        }

        foreach ((HolidayRule rule, DateOnly date) in deferred.OrderBy(static entry => entry.Date))
        {
            DateOnly substitute = Substitute(rule.Substitution, date, occupied);
            occupied.Add(substitute);
            holidays.Add(Create(rule, substitute, isSubstitute: true));
        }

        return
        [
            .. holidays
                .Where(holiday => holiday.Date.Year == year)
                .OrderBy(static holiday => holiday.Date)
                .ThenBy(static holiday => holiday.Name, StringComparer.Ordinal)
        ];
    }

    private DateOnly Substitute(
        SubstitutionPolicy policy,
        DateOnly date,
        HashSet<DateOnly> occupied)
    {
        int direction = policy == SubstitutionPolicy.NearestWorkday
            && date.DayOfWeek == System.DayOfWeek.Saturday
                ? -1
                : 1;

        return Walk(date, direction, occupied);
    }

    private DateOnly Walk(DateOnly from, int direction, HashSet<DateOnly> occupied)
    {
        DateOnly candidate = from;

        for (int guard = 0; guard < 14; guard++)
        {
            candidate = candidate.AddDays(direction);

            if (!Weekend.Includes(candidate) && !occupied.Contains(candidate))
            {
                return candidate;
            }
        }

        return candidate;
    }

    private Holiday Create(HolidayRule rule, DateOnly date, bool isSubstitute) => new()
    {
        Date = date,
        Name = rule.Name,
        LocalName = rule.LocalName,
        CountryCode = Code,
        Types = rule.Types,
        IsSubstitute = isSubstitute,
        IsEstimated = rule.IsEstimated
    };
}
