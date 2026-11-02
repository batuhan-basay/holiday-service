namespace HolidayService.Domain.Common;

/// <summary>
/// Classifies a failure so transport layers can map it without inspecting error codes.
/// </summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unavailable = 4
}
