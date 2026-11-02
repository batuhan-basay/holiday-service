using FluentValidation;
using HolidayService.Application.Behaviors;
using HolidayService.Application.Holidays;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayService.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers handlers, validators and the request pipeline. Behaviors run in registration
    /// order, so logging wraps validation and therefore also sees malformed requests.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(ApplicationAssembly.Reference);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(
            ApplicationAssembly.Reference,
            includeInternalTypes: true);

        services.AddScoped<HolidayRangeLoader>();

        return services;
    }
}
