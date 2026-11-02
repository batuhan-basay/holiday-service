namespace HolidayService.Application.Exceptions;

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
