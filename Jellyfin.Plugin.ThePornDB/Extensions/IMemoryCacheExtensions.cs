using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.InMemory;

namespace ThePornDB.Extensions
{
    internal static class IMemoryCacheExtensions
    {
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
                return Task.FromResult(default(CacheData));
            }
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
    }
}
