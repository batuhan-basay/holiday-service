namespace HolidayService.Application.Abstractions.Caching;

/// <summary>
/// Cache abstraction owned by the application layer so the caching decorator does not
/// depend on a concrete provider (in-memory today, Redis without touching callers).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Returns the cached entry or produces it with <paramref name="factory"/>, guaranteeing
    /// that concurrent callers for the same key trigger only one upstream call.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
