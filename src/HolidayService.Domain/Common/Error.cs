namespace HolidayService.Domain.Common;

/// <summary>
/// A machine-readable failure. Business outcomes travel as values, not exceptions.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unavailable(string code, string description) =>
        new(code, description, ErrorType.Unavailable);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);
}
