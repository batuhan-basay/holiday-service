namespace HolidayService.Application.Contracts;

public sealed record CountryResponse(string Code, string Name);

public sealed record CountryDetailResponse(
    string Code,
    string Name,
    string OfficialName,
    IReadOnlyList<string> WeekendDays);
