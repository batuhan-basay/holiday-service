using System.ComponentModel.DataAnnotations;

namespace HolidayService.Api.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public const string PerClientPolicy = "per-client";

    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    [Range(0, 1000)]
    public int QueueLimit { get; set; }
}
