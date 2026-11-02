using System.ComponentModel.DataAnnotations;

namespace HolidayService.Api.RateLimiting;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public const string PerClientPolicy = "per-client";

    /// <summary>Requests allowed per window, per API key (or per IP when unauthenticated).</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 100;

    /// <summary>Length of the fixed window in seconds.</summary>
    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Requests allowed to wait for the next window instead of being rejected.</summary>
    [Range(0, 1000)]
    public int QueueLimit { get; set; }
}
