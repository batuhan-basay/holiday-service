using HolidayService.Application.Abstractions.Caching;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Infrastructure.Caching;
using HolidayService.Infrastructure.Calendars;
using HolidayService.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HolidayService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<HolidayDataOptions>()
            .Bind(configuration.GetSection(HolidayDataOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<EmbeddedCalendarCatalog>();
        services.AddSingleton<CalendarHolidayProvider>();

        services.AddSingleton<IHolidayProvider>(provider => new CachedHolidayProvider(
            provider.GetRequiredService<CalendarHolidayProvider>(),
            provider.GetRequiredService<ICacheService>(),
            provider.GetRequiredService<IOptions<HolidayDataOptions>>()));

        return services;
    }
}
