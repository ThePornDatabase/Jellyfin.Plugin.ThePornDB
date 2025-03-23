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

namespace ThePornDB.Helpers.Utils
{
    internal class RateLimitCachingHandler : DelegatingHandler
    {
        private static readonly HashSet<HttpMethod> CachedHttpMethods = new HashSet<HttpMethod>
        {
            HttpMethod.Get,
            HttpMethod.Head,
        };

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

        public void InvalidateCache(Uri uri, HttpMethod httpMethod = null)
        {
            var httpMethods = httpMethod != null ? new HashSet<HttpMethod> { httpMethod } : CachedHttpMethods;

            foreach (var method in httpMethods)
            {
                var httpRequestMessage = new HttpRequestMessage(method, uri);
                var key = this.CacheKeysProvider.GetKey(httpRequestMessage);
                this.responseCache.Remove(key);
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string key = null;

            var isCachedHttpMethod = CachedHttpMethods.Contains(request.Method);
            if (isCachedHttpMethod)
            {
                key = this.CacheKeysProvider.GetKey(request);
                if (this.TryGetCachedHttpResponseMessage(request, key, out var cachedResponse))
                {
                    return cachedResponse;
                }
            }

            await this.rateLimiter;

            var response = await base.SendAsync(request, cancellationToken).TimeoutWithResult((int)TimeSpan.FromSeconds(120).TotalMilliseconds);
            if (isCachedHttpMethod)
            {
                var absoluteExpirationRelativeToNow = response.StatusCode.GetAbsoluteExpirationRelativeToNow(this.cacheExpirationPerHttpResponseCode);

                this.StatsProvider.ReportCacheMiss(response.StatusCode);
                if (absoluteExpirationRelativeToNow != TimeSpan.Zero)
                {
                    var entry = await response.ToCacheEntryAsync();
                    await this.responseCache.TrySetAsync(key, entry, absoluteExpirationRelativeToNow);
                    return request.PrepareCachedEntry(entry);
                }
            }

            return response;
        }

        private bool TryGetCachedHttpResponseMessage(HttpRequestMessage request, string key, out HttpResponseMessage cachedResponse)
        {
            if (this.responseCache.TryGetCacheData(key, out var cacheData))
            {
                cachedResponse = request.PrepareCachedEntry(cacheData);
                this.StatsProvider.ReportCacheHit(cachedResponse.StatusCode);
                return true;
            }

            cachedResponse = default;
            return false;
        }
    }
}
