using HolidayService.Domain.Calendar;

namespace HolidayService.Domain.Holidays;

public abstract record HolidayRule
{
    public required string Name { get; init; }

    public required string LocalName { get; init; }

    public IReadOnlyList<HolidayType> Types { get; init; } = [HolidayType.Public];

    public SubstitutionPolicy Substitution { get; init; } = SubstitutionPolicy.None;

    public virtual bool IsEstimated => false;

    public abstract IEnumerable<DateOnly> OccurrencesIn(int year);
}

public sealed record FixedDateRule : HolidayRule
{
    public required int Month { get; init; }

    public required int Day { get; init; }

    public override IEnumerable<DateOnly> OccurrencesIn(int year)
    {
        if (Month is < 1 or > 12 || Day < 1 || Day > DateTime.DaysInMonth(year, Month))
        {
            yield break;
        }

        yield return new DateOnly(year, Month, Day);
    }
}

public sealed record NthWeekdayRule : HolidayRule
{
    public required int Month { get; init; }

    public required DayOfWeek DayOfWeek { get; init; }

    public required int Occurrence { get; init; }

    public override IEnumerable<DateOnly> OccurrencesIn(int year)
    {
        if (Month is < 1 or > 12 || Occurrence == 0 || Occurrence is < -1 or > 5)
        {
            yield break;
        }

        int daysInMonth = DateTime.DaysInMonth(year, Month);

        var matching = new List<DateOnly>();

        for (int day = 1; day <= daysInMonth; day++)
        {
            var candidate = new DateOnly(year, Month, day);

            if (candidate.DayOfWeek == DayOfWeek)
            {
                matching.Add(candidate);
            }
        }

        if (Occurrence == -1)
        {
            yield return matching[^1];
            yield break;
        }

        if (Occurrence <= matching.Count)
        {
            yield return matching[Occurrence - 1];
        }
    }
}

public sealed record EasterOffsetRule : HolidayRule
{
    public required int OffsetDays { get; init; }

    public override IEnumerable<DateOnly> OccurrencesIn(int year)
    {
        DateOnly easter = EasterCalculator.EasterSunday(year);
        DateOnly occurrence = easter.AddDays(OffsetDays);

        if (occurrence.Year == year)
        {
            yield return occurrence;
        }
    }
}

public sealed record HijriRule : HolidayRule
{
    public override bool IsEstimated => true;

    public required int HijriMonth { get; init; }

    public required int HijriDay { get; init; }

    public int Length { get; init; } = 1;

    public int DayAdjustment { get; init; }

    public override IEnumerable<DateOnly> OccurrencesIn(int year)
    {
        IEnumerable<DateOnly> starts = HijriCalculator
            .OccurrencesIn(year - 1, HijriMonth, HijriDay, DayAdjustment)
            .Concat(HijriCalculator.OccurrencesIn(year, HijriMonth, HijriDay, DayAdjustment));

        foreach (DateOnly start in starts)
        {
            for (int offset = 0; offset < Math.Max(Length, 1); offset++)
            {
                DateOnly occurrence = start.AddDays(offset);

                if (occurrence.Year == year)
                {
                    yield return occurrence;
                }
            }
        }
    }
}
