namespace HolidayService.Domain.Countries;

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
