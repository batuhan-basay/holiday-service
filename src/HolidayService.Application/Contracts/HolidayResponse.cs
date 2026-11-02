namespace HolidayService.Application.Contracts;

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

public sealed record HolidayCollectionResponse(
    string CountryCode,
    DateOnly From,
    DateOnly To,
    int Count,
    IReadOnlyList<HolidayResponse> Holidays);

public sealed record DayStatusResponse(
    string CountryCode,
    DateOnly Date,
    string DayOfWeek,
    bool IsPublicHoliday,
    bool IsWeekend,
    bool IsWorkday,
    HolidayResponse? Holiday);

public sealed record LongWeekendResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    int DayCount,
    bool NeedsBridgeDay);
