namespace HolidayService.Domain.Countries;

public sealed record Country(
    CountryCode Code,
    string Name,
    string OfficialName,
    IReadOnlyList<DayOfWeek> WeekendDays);
