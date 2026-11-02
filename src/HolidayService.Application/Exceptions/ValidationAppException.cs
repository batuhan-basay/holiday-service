namespace HolidayService.Application.Exceptions;

/// <summary>
/// Raised by the validation pipeline behavior when a request is malformed. Input validation
/// is a programmer/caller error rather than a business outcome, so it travels as an
/// exception and is translated to a 400 by the API layer, while business outcomes use Result.
/// </summary>
public sealed class ValidationAppException : Exception
{
    public ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.") => Errors = errors;

    public ValidationAppException()
        : this(new Dictionary<string, string[]>())
    {
    }

    public ValidationAppException(string message)
        : base(message) => Errors = new Dictionary<string, string[]>();

    public ValidationAppException(string message, Exception innerException)
        : base(message, innerException) => Errors = new Dictionary<string, string[]>();

    public IReadOnlyDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
}
