using HolidayService.Domain.Countries;
using HolidayService.Domain.Holidays;

namespace HolidayService.Infrastructure.Nager;

internal static class NagerMapper
{
    public static Holiday? ToDomain(this NagerPublicHoliday dto, CountryCode fallbackCountry)
    {
        ArgumentNullException.ThrowIfNull(dto);

        CountryCode countryCode = CountryCode.TryCreate(dto.CountryCode, out CountryCode? parsed)
            ? parsed.Value
            : fallbackCountry;

        string name = dto.Name ?? dto.LocalName ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new Holiday
        {
            Date = dto.Date,
            Name = name,
            LocalName = dto.LocalName ?? name,
            CountryCode = countryCode,
            IsFixed = dto.Fixed,
            IsGlobal = dto.Global,
            Counties = dto.Counties ?? [],
            LaunchYear = dto.LaunchYear,
            Types = ToHolidayTypes(dto.Types)
        };
    }

    public static CountrySummary? ToDomain(this NagerCountry dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return CountryCode.TryCreate(dto.CountryCode, out CountryCode? code)
            ? new CountrySummary(code.Value, dto.Name ?? code.Value.Value)
            : null;
    }

    public static Country? ToDomain(this NagerCountryInfo dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!CountryCode.TryCreate(dto.CountryCode, out CountryCode? code))
        {
            return null;
        }

        string commonName = dto.CommonName ?? dto.OfficialName ?? code.Value.Value;

        IReadOnlyList<CountrySummary> borders =
        [
            .. (dto.Borders ?? [])
                .Select(static border =>
                    CountryCode.TryCreate(border.CountryCode, out CountryCode? borderCode)
                        ? new CountrySummary(
                            borderCode.Value,
                            border.CommonName ?? borderCode.Value.Value)
                        : null)
                .Where(static summary => summary is not null)
                .Select(static summary => summary!)
        ];

        return new Country(
            code.Value,
            commonName,
            dto.OfficialName ?? commonName,
            dto.Region,
            borders);
    }

    private static IReadOnlyList<HolidayType> ToHolidayTypes(IReadOnlyList<string>? types)
    {
        if (types is null || types.Count == 0)
        {
            return [HolidayType.Public];
        }

        return
        [
            .. types
                .Select(static type =>
                    Enum.TryParse(type, ignoreCase: true, out HolidayType parsed)
                        ? parsed
                        : HolidayType.Unknown)
                .Distinct()
        ];
    }
}
