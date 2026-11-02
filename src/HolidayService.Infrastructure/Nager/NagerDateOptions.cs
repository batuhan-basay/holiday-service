using System.ComponentModel.DataAnnotations;

namespace HolidayService.Infrastructure.Nager;

public sealed class NagerDateOptions
{
    public const string SectionName = "NagerDate";

    [Required]
    public Uri BaseAddress { get; set; } = new("https://date.nager.at/");

    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    [Range(typeof(TimeSpan), "00:00:10", "7.00:00:00")]
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(12);

    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan HistoricalCacheDuration { get; set; } = TimeSpan.FromDays(7);

    [Range(1, 5)]
    public int RetryCount { get; set; } = 3;
}
