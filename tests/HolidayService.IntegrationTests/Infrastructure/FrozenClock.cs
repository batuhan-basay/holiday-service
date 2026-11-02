using HolidayService.Application.Abstractions.Time;

namespace HolidayService.IntegrationTests.Infrastructure;

internal sealed class FrozenClock(DateOnly today) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } =
        new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public DateOnly Today { get; } = today;
}
