using System.Reflection;

namespace HolidayService.Application;

public static class ApplicationAssembly
{
    public static Assembly Reference { get; } = typeof(ApplicationAssembly).Assembly;
}
