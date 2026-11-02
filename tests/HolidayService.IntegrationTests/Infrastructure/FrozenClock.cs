using HolidayService.Application.Abstractions.Time;

namespace HolidayService.IntegrationTests.Infrastructure;

/// <summary>
/// Freezes "today" so that endpoints built on the current date (upcoming, next, shift)
/// assert against fixed expectations instead of drifting with the wall clock.
/// </summary>
internal sealed class FrozenClock(DateOnly today) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } =
        new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public DateOnly Today { get; } = today;
}
