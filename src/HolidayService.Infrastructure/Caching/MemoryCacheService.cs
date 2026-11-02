using System.Collections.Concurrent;
using HolidayService.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace HolidayService.Infrastructure.Caching;

internal sealed class MemoryCacheService(IMemoryCache cache) : ICacheService, IDisposable
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new(StringComparer.Ordinal);

    private bool _disposed;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(cache.TryGetValue(key, out T? value) ? value : null);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        cache.Set(key, value, expiration ?? DefaultExpiration);

        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        SemaphoreSlim keyLock = _keyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            T produced = await factory(cancellationToken).ConfigureAwait(false);

            cache.Set(key, produced, expiration ?? DefaultExpiration);

            return produced;
        }
        finally
        {
            keyLock.Release();
            _keyLocks.TryRemove(key, out _);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        cache.Remove(key);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (SemaphoreSlim keyLock in _keyLocks.Values)
        {
            keyLock.Dispose();
        }

        _keyLocks.Clear();
        _disposed = true;
    }
}
