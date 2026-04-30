using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;

namespace Migration.Connectors.Targets.Bynder.Extensions;

public static class MemoryCacheExtensions
{
    private static readonly ConcurrentDictionary<object, SemaphoreSlim> Locks = new();

    public static async Task<TItem?> GetOrCreateAsyncWithLock<TItem>(this IMemoryCache cache, object key, Func<ICacheEntry, Task<TItem>> factory)
    {
        if (cache.TryGetValue<TItem>(key, out var value))
        {
            return value;
        }

        var cacheLock = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }

            return await cache.GetOrCreateAsync(key, factory).ConfigureAwait(false);
        }
        finally
        {
            cacheLock.Release();
        }
    }
}
