namespace HolidayService.Application.Abstractions.Time;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}
