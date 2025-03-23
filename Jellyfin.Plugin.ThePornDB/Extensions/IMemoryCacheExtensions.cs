using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.InMemory;

internal static class IMemoryCacheExtensions
{
    [Obsolete("Use TryGetCacheData")]
    public static Task<CacheData> TryGetAsync(this IMemoryCache cache, string key)
    {
        try
        {
            if (cache.TryGetValue(key, out byte[] binaryData))
            {
                return Task.FromResult(binaryData.Deserialize());
            }

            return Task.FromResult(default(CacheData));
        }
        catch (Exception)
        {
            // ignore all exceptions; return null
            return Task.FromResult(default(CacheData));
        }
    }

    public static bool TryGetCacheData(this IMemoryCache cache, string key, out CacheData cacheData)
    {
        var result = false;
        cacheData = default;

        try
        {
            if (cache.TryGetValue(key, out byte[] binaryData))
            {
                cacheData = binaryData.Deserialize();
                result = true;
            }
        }
        catch
        {
        }

        return result;
    }

    public static Task TrySetAsync(this IMemoryCache cache, string key, CacheData value, TimeSpan absoluteExpirationRelativeToNow)
    {
        try
        {
            cache.Set(key, value.Serialize(), absoluteExpirationRelativeToNow);
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }

    public static bool TrySetCacheData(this IMemoryCache cache, string key, CacheData value, TimeSpan absoluteExpirationRelativeToNow)
    {
        bool result;

        try
        {
            cache.Set(key, value.Serialize(), absoluteExpirationRelativeToNow);
            result = true;
        }
        catch
        {
            result = false;
        }

        return result;
    }
}
