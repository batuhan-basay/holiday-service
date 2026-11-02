namespace HolidayService.Domain.Holidays;

/// <summary>Holiday classification as published by the upstream provider.</summary>
public enum HolidayType
{
    Unknown = 0,
    Public = 1,
    Bank = 2,
    School = 3,
    Authorities = 4,
    Optional = 5,
    Observance = 6
}
