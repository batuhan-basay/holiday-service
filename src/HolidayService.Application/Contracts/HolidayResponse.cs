namespace HolidayService.Application.Contracts;

/// <summary>A single holiday occurrence.</summary>
public sealed record HolidayResponse(
    DateOnly Date,
    string Name,
    string LocalName,
    string CountryCode,
    string DayOfWeek,
    bool IsFixed,
    bool IsGlobal,
    IReadOnlyList<string> Counties,
    int? LaunchYear,
    IReadOnlyList<string> Types);

/// <summary>A set of holidays plus the query echo that produced it.</summary>
public sealed record HolidayCollectionResponse(
    string CountryCode,
    DateOnly From,
    DateOnly To,
    int Count,
    IReadOnlyList<HolidayResponse> Holidays);

/// <summary>Answer to "is this date off work?".</summary>
public sealed record DayStatusResponse(
    string CountryCode,
    DateOnly Date,
    string DayOfWeek,
    bool IsPublicHoliday,
    bool IsWeekend,
    bool IsWorkday,
    HolidayResponse? Holiday);

/// <summary>A stretch of consecutive days off.</summary>
public sealed record LongWeekendResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int DayCount,
    bool NeedsBridgeDay);
