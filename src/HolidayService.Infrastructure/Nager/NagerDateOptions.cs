using System.ComponentModel.DataAnnotations;

namespace HolidayService.Infrastructure.Nager;

/// <summary>Configuration for the upstream Nager.Date integration and its cache policy.</summary>
public sealed class NagerDateOptions
{
    public const string SectionName = "NagerDate";

    /// <summary>Base address of the upstream API.</summary>
    [Required]
    public Uri BaseAddress { get; set; } = new("https://date.nager.at/");

    /// <summary>Total budget for one upstream call, retries included.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How long the current and future years stay cached; they can still change.</summary>
    [Range(typeof(TimeSpan), "00:00:10", "7.00:00:00")]
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(12);

    /// <summary>Past years are immutable in practice, so they are cached far longer.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "30.00:00:00")]
    public TimeSpan HistoricalCacheDuration { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Retry attempts performed by the resilience pipeline on transient failures.
    /// The underlying Polly strategy requires at least one attempt.
    /// </summary>
    [Range(1, 5)]
    public int RetryCount { get; set; } = 3;
}
