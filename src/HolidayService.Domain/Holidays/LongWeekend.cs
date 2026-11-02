namespace HolidayService.Domain.Holidays;

/// <summary>
/// A consecutive stretch of non-working days. <see cref="NeedsBridgeDay"/> marks the
/// stretches that only materialise if the employee takes one day off.
/// </summary>
public sealed record LongWeekend(DateOnly StartDate, DateOnly EndDate, bool NeedsBridgeDay)
{
    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;
}
