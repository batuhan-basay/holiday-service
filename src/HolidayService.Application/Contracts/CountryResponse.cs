namespace HolidayService.Application.Contracts;

/// <summary>Country as returned by listing endpoints.</summary>
/// <param name="Code">ISO 3166-1 alpha-2 code, e.g. "TR".</param>
/// <param name="Name">English country name.</param>
public sealed record CountryResponse(string Code, string Name);

/// <summary>Full country profile.</summary>
public sealed record CountryDetailResponse(
    string Code,
    string CommonName,
    string OfficialName,
    string? Region,
    IReadOnlyList<CountryResponse> Borders);
