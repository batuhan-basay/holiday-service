namespace HolidayService.Domain.Holidays;

public sealed record LongWeekend(DateOnly StartDate, DateOnly EndDate, bool NeedsBridgeDay)
{
    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;
}
