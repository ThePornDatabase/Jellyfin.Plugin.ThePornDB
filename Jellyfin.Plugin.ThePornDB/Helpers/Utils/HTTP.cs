using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ComposableAsync;
using Microsoft.Extensions.Caching.Abstractions;
using RateLimiter;

namespace ThePornDB.Helpers.Utils
{
    internal static class HTTP
    {
        static HTTP()
        {
            Http.Timeout = Timeout.InfiniteTimeSpan;
        }

        private static CookieContainer CookieContainer { get; } = new CookieContainer();

#if __EMBY__
        private static StandardSocketsHttpHandler SocketsHttpHandler { get; } = new StandardSocketsHttpHandler()
#else
        private static SocketsHttpHandler SocketsHttpHandler { get; } = new SocketsHttpHandler()
#endif
        {
            CookieContainer = CookieContainer,
            PooledConnectionLifetime = TimeSpan.FromSeconds(300),
        };

        private static IDictionary<HttpStatusCode, TimeSpan> CacheExpirationPerHttpResponseCode { get; } = CacheExpirationProvider.CreateSimple(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        private static RateLimitCachingHandler RateLimitCachingHandler { get; } = new RateLimitCachingHandler(SocketsHttpHandler, CacheExpirationPerHttpResponseCode, TimeLimiter.GetFromMaxCountByInterval(120, TimeSpan.FromSeconds(60)));

        private static HttpClient Http { get; } = new HttpClient(RateLimitCachingHandler);

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, HttpContent param, IDictionary<string, string> headers, IDictionary<string, string> cookies, CancellationToken cancellationToken)
        {
            var result = new HTTPResponse()
            {
                IsOK = false,
            };

            if (method == null)
            {
                method = HttpMethod.Get;
            }

            var request = new HttpRequestMessage(method, new Uri(url));

            Logger.Debug(string.Format(CultureInfo.InvariantCulture, "Requesting {1} \"{0}\"", request.RequestUri.AbsoluteUri, method.Method));

            request.Headers.TryAddWithoutValidation("User-Agent", Consts.UserAgent);

            if (param != null)
            {
                request.Content = param;
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            if (cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    CookieContainer.Add(request.RequestUri, new Cookie(cookie.Key, cookie.Value));
                }
            }

            HttpResponseMessage response = null;
            try
            {
                response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Request error: {e.Message}");
            }

            if (response != null)
            {
                result.IsOK = response.IsSuccessStatusCode;
#if __EMBY__
                result.Content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#else
                result.Content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                result.ContentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#endif
                result.Headers = response.Headers;
                result.Cookies = CookieContainer.GetCookies(request.RequestUri).Cast<Cookie>();
            }

            return result;
        }

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, HttpContent param, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, method, param, headers, cookies, cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, HttpMethod method, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, method, null, headers, cookies, cancellationToken).ConfigureAwait(false);

        public static async Task<HTTPResponse> Request(string url, CancellationToken cancellationToken, IDictionary<string, string> headers = null, IDictionary<string, string> cookies = null)
            => await Request(url, null, null, headers, cookies, cancellationToken).ConfigureAwait(false);

        internal struct HTTPResponse
        {
            public string Content { get; set; }

            public Stream ContentStream { get; set; }

            public bool IsOK { get; set; }

            public IEnumerable<Cookie> Cookies { get; set; }

            public HttpResponseHeaders Headers { get; set; }
        }
    }
}
