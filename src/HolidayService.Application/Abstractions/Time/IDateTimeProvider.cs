namespace HolidayService.Application.Abstractions.Time;

/// <summary>Clock abstraction so "today"-based logic stays deterministic under test.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
