using System.Reflection;

namespace HolidayService.Application;

/// <summary>Anchor type for assembly scanning (MediatR handlers, FluentValidation validators).</summary>
public static class ApplicationAssembly
{
    public static Assembly Reference { get; } = typeof(ApplicationAssembly).Assembly;
}
