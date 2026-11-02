using FluentValidation;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Behaviors;
using HolidayService.Application.Holidays;
using HolidayService.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace HolidayService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        services.AddScoped(typeof(IQueryBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IQueryBehavior<,>), typeof(ValidationBehavior<,>));

        AddQueryHandlers(services);

        services.AddValidatorsFromAssembly(
            ApplicationAssembly.Reference,
            includeInternalTypes: true);

        services.AddScoped<HolidayRangeLoader>();

        return services;
    }

    private static void AddQueryHandlers(IServiceCollection services)
    {
        foreach (Type type in ApplicationAssembly.Reference.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (Type contract in type.GetInterfaces())
            {
                if (contract.IsGenericType
                    && contract.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                {
                    services.AddScoped(contract, type);
                }
            }
        }
    }
}
