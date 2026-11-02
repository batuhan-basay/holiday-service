using System.ComponentModel.DataAnnotations;

namespace HolidayService.Infrastructure.Calendars;

public sealed class HolidayDataOptions
{
    public const string SectionName = "HolidayData";

    [Range(typeof(TimeSpan), "00:00:10", "30.00:00:00")]
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(12);

    [Range(-2, 2)]
    public int HijriDayAdjustment { get; set; }
}
