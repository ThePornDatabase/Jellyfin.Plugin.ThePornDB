using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ComposableAsync;
using Microsoft.Extensions.Caching.Abstractions;
using Microsoft.Extensions.Caching.InMemory;
using RateLimiter;
using ThePornDB.Extensions;

namespace ThePornDB.Helpers.Utils
{
    internal class RateLimitCachingHandler : DelegatingHandler
    {
        private readonly IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode;
        private readonly IMemoryCache responseCache;
        private readonly TimeLimiter rateLimiter;

        public RateLimitCachingHandler(HttpMessageHandler innerHandler = null, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode = null, TimeLimiter timeLimiter = null, IStatsProvider statsProvider = null, ICacheKeysProvider cacheKeysProvider = null)
            : this(innerHandler, cacheExpirationPerHttpResponseCode, timeLimiter, statsProvider, new MemoryCache(new MemoryCacheOptions()), cacheKeysProvider)
        {
        }

        internal RateLimitCachingHandler(HttpMessageHandler innerHandler, IDictionary<HttpStatusCode, TimeSpan> cacheExpirationPerHttpResponseCode, TimeLimiter timeLimiter, IStatsProvider statsProvider, IMemoryCache cache, ICacheKeysProvider cacheKeysProvider)
            : base(innerHandler ?? new HttpClientHandler())
        {
            this.StatsProvider = statsProvider ?? new StatsProvider(nameof(InMemoryCacheHandler));
            this.cacheExpirationPerHttpResponseCode = cacheExpirationPerHttpResponseCode ?? new Dictionary<HttpStatusCode, TimeSpan>();
            this.responseCache = cache ?? new MemoryCache(new MemoryCacheOptions());
            this.rateLimiter = timeLimiter ?? TimeLimiter.GetFromMaxCountByInterval(120, TimeSpan.FromSeconds(60));
            this.CacheKeysProvider = cacheKeysProvider ?? new DefaultCacheKeysProvider();
        }

        public IStatsProvider StatsProvider { get; }

        public ICacheKeysProvider CacheKeysProvider { get; }

        public void InvalidateCache(Uri uri, HttpMethod method = null)
        {
            var methods = method != null ? new[] { method } : new[] { HttpMethod.Get, HttpMethod.Head };
            foreach (var m in methods)
            {
                var request = new HttpRequestMessage(m, uri);
                var key = this.CacheKeysProvider.GetKey(request);
                this.responseCache.Remove(key);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = this.CacheKeysProvider.GetKey(request);
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
            {
                var data = await this.responseCache.TryGetAsync(key);
                if (data != null)
                {
                    var cachedResponse = request.PrepareCachedEntry(data);
                    this.StatsProvider.ReportCacheHit(cachedResponse.StatusCode);
                    return cachedResponse;
                }
            }

            await this.rateLimiter;

            var response = await base.SendAsync(request, cancellationToken);
            if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head)
            {
                var absoluteExpirationRelativeToNow = response.StatusCode.GetAbsoluteExpirationRelativeToNow(this.cacheExpirationPerHttpResponseCode);

                this.StatsProvider.ReportCacheMiss(response.StatusCode);

                if (absoluteExpirationRelativeToNow != TimeSpan.Zero)
                {
                    var entry = await response.ToCacheEntry();
                    await this.responseCache.TrySetAsync(key, entry, absoluteExpirationRelativeToNow);
                    return request.PrepareCachedEntry(entry);
                }
            }

            return response;
        }
    }
}
