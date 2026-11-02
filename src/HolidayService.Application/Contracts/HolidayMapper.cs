using System.Globalization;
using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Application.Contracts;

/// <summary>
/// Hand-written domain to contract mapping. Explicit mapping keeps the wire format an
/// intentional decision rather than a side effect of renaming a domain property.
/// </summary>
public static class HolidayMapper
{
    public static HolidayResponse ToResponse(this Holiday holiday)
    {
        ArgumentNullException.ThrowIfNull(holiday);

        return new HolidayResponse(
            holiday.Date,
            holiday.Name,
            holiday.LocalName,
            holiday.CountryCode.Value,
            holiday.DayOfWeek.ToString(),
            holiday.IsFixed,
            holiday.IsGlobal,
            holiday.Counties,
            holiday.LaunchYear,
            [.. holiday.Types.Select(static type => type.ToString())]);
    }

    public static IReadOnlyList<HolidayResponse> ToResponse(this IEnumerable<Holiday> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);

        return [.. holidays.Select(ToResponse)];
    }

    public static CountryResponse ToResponse(this CountrySummary country)
    {
        ArgumentNullException.ThrowIfNull(country);

        return new CountryResponse(country.Code.Value, country.Name);
    }

    public static CountryDetailResponse ToResponse(this Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        return new CountryDetailResponse(
            country.Code.Value,
            country.CommonName,
            country.OfficialName,
            country.Region,
            [.. country.Borders.Select(ToResponse)]);
    }

    public static LongWeekendResponse ToResponse(this LongWeekend longWeekend)
    {
        ArgumentNullException.ThrowIfNull(longWeekend);

        return new LongWeekendResponse(
            longWeekend.StartDate,
            longWeekend.EndDate,
            longWeekend.DayCount,
            longWeekend.NeedsBridgeDay);
    }

    public static string ToMonthName(int month) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}
