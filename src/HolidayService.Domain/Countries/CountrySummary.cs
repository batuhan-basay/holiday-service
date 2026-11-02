
namespace HolidayService.Domain.Countries;

/// <summary>Minimal country projection used by listing endpoints.</summary>
public sealed record CountrySummary(CountryCode Code, string Name);
