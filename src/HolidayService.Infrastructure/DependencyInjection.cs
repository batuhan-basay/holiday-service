using HolidayService.Application.Abstractions.Caching;
using HolidayService.Application.Abstractions.Providers;
using HolidayService.Application.Abstractions.Time;
using HolidayService.Infrastructure.Caching;
using HolidayService.Infrastructure.Nager;
using HolidayService.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HolidayService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the upstream client, its resilience pipeline and the caching decorator.
    /// <see cref="IHolidayProvider"/> resolves to the cached decorator; the raw client is
    /// only reachable by the decorator itself.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<NagerDateOptions>()
            .Bind(configuration.GetSection(NagerDateOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        NagerDateOptions settings = configuration
            .GetSection(NagerDateOptions.SectionName)
            .Get<NagerDateOptions>() ?? new NagerDateOptions();

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services
            .AddHttpClient<NagerHolidayProvider>(client =>
            {
                client.BaseAddress = settings.BaseAddress;
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = settings.RetryCount;
                resilience.Retry.UseJitter = true;
                resilience.AttemptTimeout.Timeout = settings.Timeout;
                resilience.TotalRequestTimeout.Timeout =
                    TimeSpan.FromTicks(settings.Timeout.Ticks * 3);
                resilience.CircuitBreaker.SamplingDuration =
                    TimeSpan.FromTicks(settings.Timeout.Ticks * 4);
            });

        services.AddTransient<IHolidayProvider>(provider => new CachedHolidayProvider(
            provider.GetRequiredService<NagerHolidayProvider>(),
            provider.GetRequiredService<ICacheService>(),
            provider.GetRequiredService<IDateTimeProvider>(),
            provider.GetRequiredService<IOptions<NagerDateOptions>>()));

        return services;
    }
}
