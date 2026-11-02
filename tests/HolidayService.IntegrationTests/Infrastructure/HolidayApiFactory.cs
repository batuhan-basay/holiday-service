using System.Net.Http.Headers;
using HolidayService.Application.Abstractions.Time;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.Server;

namespace HolidayService.IntegrationTests.Infrastructure;

public sealed class HolidayApiFactory : WebApplicationFactory<Program>
{
    private const string _apiKey = "integration-test-key";

    private static readonly DateOnly _today = new(2026, 3, 2);

    public HolidayApiFactory() => Upstream = WireMockServer.Start();

    public WireMockServer Upstream { get; }

    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    public void ResetState()
    {
        Upstream.Reset();

        if (Services.GetRequiredService<IMemoryCache>() is MemoryCache cache)
        {
            cache.Clear();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["NagerDate:BaseAddress"] = $"{Upstream.Url}/",
                ["NagerDate:Timeout"] = "00:00:05",
                ["NagerDate:RetryCount"] = "1",
                ["NagerDate:CacheDuration"] = "00:05:00",
                ["NagerDate:HistoricalCacheDuration"] = "00:10:00",
                ["ApiKeys:Enabled"] = "true",
                ["ApiKeys:Clients:0:Owner"] = "integration-tests",
                ["ApiKeys:Clients:0:Key"] = _apiKey,
                ["RateLimiting:PermitLimit"] = "10000",
                ["RateLimiting:WindowSeconds"] = "60"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(new FrozenClock(_today));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Upstream.Stop();
            Upstream.Dispose();
        }

        base.Dispose(disposing);
    }
}
