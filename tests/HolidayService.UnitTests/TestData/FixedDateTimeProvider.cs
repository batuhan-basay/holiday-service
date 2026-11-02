using HolidayService.Application.Abstractions.Time;

namespace HolidayService.UnitTests.TestData;

internal sealed class FixedDateTimeProvider(DateOnly today) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } =
        new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public DateOnly Today { get; } = today;
}
