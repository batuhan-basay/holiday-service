using System.Net.Http.Headers;
using HolidayService.Application.Abstractions.Time;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WireMock.Server;

namespace HolidayService.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real request pipeline — routing, authentication, rate limiting, validation,
/// MediatR, caching — against a stubbed upstream provider. Nothing is mocked inside the
/// application itself, so these tests fail if any of that wiring breaks.
/// <para>
/// A WireMock server stands in for Nager.Date. There is no database to containerise, so a
/// stubbed HTTP boundary is the equivalent of a Testcontainers dependency here.
/// </para>
/// </summary>
public sealed class HolidayApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "integration-test-key";

    /// <summary>The date every date-relative endpoint is evaluated against.</summary>
    public static readonly DateOnly Today = new(2026, 3, 2);

    public HolidayApiFactory() => Upstream = WireMockServer.Start();

    /// <summary>The stubbed Nager.Date instance the API talks to.</summary>
    public WireMockServer Upstream { get; }

    /// <summary>An authenticated client with the test API key already attached.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    /// <summary>
    /// Clears the stubbed mappings and the API's in-process cache so each test starts from a
    /// cold, empty state even though the host is shared across the class.
    /// </summary>
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
                ["ApiKeys:Clients:0:Key"] = ApiKey,
                ["RateLimiting:PermitLimit"] = "10000",
                ["RateLimiting:WindowSeconds"] = "60"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(new FrozenClock(Today));
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
