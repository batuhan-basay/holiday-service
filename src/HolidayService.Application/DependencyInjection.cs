using FluentValidation;
using HolidayService.Application.Behaviors;
using HolidayService.Application.Holidays;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayService.Application;

public static class DependencyInjection
{
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
