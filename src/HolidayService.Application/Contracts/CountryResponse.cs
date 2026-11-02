namespace HolidayService.Application.Contracts;

public sealed record CountryResponse(string Code, string Name);

public sealed record CountryDetailResponse(
    string Code,
    string CommonName,
    string OfficialName,
    string? Region,
    IReadOnlyList<CountryResponse> Borders);
