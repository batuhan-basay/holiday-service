namespace HolidayService.Domain.Countries;

/// <summary>Full country profile returned by the upstream provider.</summary>
public sealed record Country(
    CountryCode Code,
    string CommonName,
    string OfficialName,
    string? Region,
    IReadOnlyList<CountrySummary> Borders)
{
    public static Country Minimal(CountryCode code, string name) =>
        new(code, name, name, Region: null, Borders: []);
}
