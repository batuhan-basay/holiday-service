using System.Globalization;
using HolidayService.Domain.Calendar;
using HolidayService.Domain.Common;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Infrastructure.Calendars;

internal static class CalendarDefinitionMapper
{
    public static HolidayCalendar ToDomain(this CalendarDefinition definition, int hijriDayAdjustment)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Result<CountryCode> code = CountryCode.Create(definition.CountryCode);

        if (code.IsFailure)
        {
            throw new InvalidOperationException(
                $"Calendar definition has an invalid country code: '{definition.CountryCode}'.");
        }

        string name = definition.Name
            ?? throw new InvalidOperationException(
                $"Calendar definition for '{definition.CountryCode}' has no name.");

        IReadOnlyList<HolidayRule> rules =
        [
            .. (definition.Holidays ?? [])
                .Select(holiday => ToRule(holiday, definition.CountryCode!, hijriDayAdjustment))
        ];

        return new HolidayCalendar(
            code.Value,
            name,
            definition.OfficialName ?? name,
            ToWeekend(definition.Weekend),
            rules);
    }

    private static WeekendDefinition ToWeekend(IReadOnlyList<string>? days)
    {
        if (days is null || days.Count == 0)
        {
            return WeekendDefinition.SaturdaySunday;
        }

        DayOfWeek[] parsed =
        [
            .. days.Select(static day => Enum.TryParse(day, ignoreCase: true, out DayOfWeek value)
                ? value
                : throw new InvalidOperationException($"'{day}' is not a day of the week."))
        ];

        return WeekendDefinition.Custom(parsed);
    }

    private static HolidayRule ToRule(
        HolidayDefinition definition,
        string countryCode,
        int hijriDayAdjustment)
    {
        string name = definition.Name
            ?? throw new InvalidOperationException($"A holiday in '{countryCode}' has no name.");

        string localName = definition.LocalName ?? name;
        IReadOnlyList<HolidayType> types = ToTypes(definition.Types);
        SubstitutionPolicy substitution = ToSubstitution(definition.Substitution);

        return definition.Kind?.ToLowerInvariant() switch
        {
            "fixed" => new FixedDateRule
            {
                Name = name,
                LocalName = localName,
                Types = types,
                Substitution = substitution,
                Month = Required(definition.Month, "month", name),
                Day = Required(definition.Day, "day", name)
            },
            "nthweekday" => new NthWeekdayRule
            {
                Name = name,
                LocalName = localName,
                Types = types,
                Substitution = substitution,
                Month = Required(definition.Month, "month", name),
                DayOfWeek = Enum.TryParse(definition.DayOfWeek, ignoreCase: true, out DayOfWeek day)
                    ? day
                    : throw new InvalidOperationException(
                        $"'{definition.DayOfWeek}' is not a day of the week ('{name}')."),
                Occurrence = Required(definition.Occurrence, "occurrence", name)
            },
            "easter" => new EasterOffsetRule
            {
                Name = name,
                LocalName = localName,
                Types = types,
                Substitution = substitution,
                OffsetDays = Required(definition.OffsetDays, "offsetDays", name)
            },
            "hijri" => new HijriRule
            {
                Name = name,
                LocalName = localName,
                Types = types,
                Substitution = substitution,
                HijriMonth = Required(definition.HijriMonth, "hijriMonth", name),
                HijriDay = Required(definition.HijriDay, "hijriDay", name),
                Length = definition.Length ?? 1,
                DayAdjustment = (definition.DayAdjustment ?? 0) + hijriDayAdjustment
            },
            _ => throw new InvalidOperationException(
                $"'{definition.Kind}' is not a known holiday rule kind ('{name}').")
        };
    }

    private static IReadOnlyList<HolidayType> ToTypes(IReadOnlyList<string>? types)
    {
        if (types is null || types.Count == 0)
        {
            return [HolidayType.Public];
        }

        return
        [
            .. types.Select(static type =>
                Enum.TryParse(type, ignoreCase: true, out HolidayType parsed)
                    ? parsed
                    : HolidayType.Unknown)
        ];
    }

    private static SubstitutionPolicy ToSubstitution(string? value) =>
        value?.ToLowerInvariant() switch
        {
            null or "" or "none" => SubstitutionPolicy.None,
            "nextworkday" => SubstitutionPolicy.NextWorkday,
            "nearestworkday" => SubstitutionPolicy.NearestWorkday,
            _ => throw new InvalidOperationException(
                $"'{value}' is not a known substitution policy.")
        };

    private static int Required(int? value, string field, string holidayName) =>
        value ?? throw new InvalidOperationException(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Holiday '{holidayName}' is missing the '{field}' field."));
}
