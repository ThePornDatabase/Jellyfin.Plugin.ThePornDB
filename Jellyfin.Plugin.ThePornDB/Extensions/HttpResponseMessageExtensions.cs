using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Abstractions;

internal static class HttpResponseMessageExtensions
{
    public static async Task<CacheData> ToCacheEntryAsync(this HttpResponseMessage httpResponseMessage)
    {
        var contentBytes = await httpResponseMessage.Content.ReadAsByteArrayAsync();
        return httpResponseMessage.ToCacheEntry(contentBytes);
    }

    public static CacheData ToCacheEntry(this HttpResponseMessage httpResponseMessage, byte[] contentBytes)
    {
        var httpResponseMessageCopy = new HttpResponseMessage
        {
            ReasonPhrase = httpResponseMessage.ReasonPhrase,
            StatusCode = httpResponseMessage.StatusCode,
            Version = httpResponseMessage.Version,
        };

        var headers = httpResponseMessage.Headers
            .Where(h => h.Value != null && h.Value.Any())
            .ToDictionary(h => h.Key, h => h.Value);

        var contentHeaders = httpResponseMessage.Content.Headers
            .Where(h => h.Value != null && h.Value.Any())
            .ToDictionary(h => h.Key, h => h.Value);

        var cacheData = new CacheData(contentBytes, httpResponseMessageCopy, headers, contentHeaders);
        return cacheData;
    }

    public static HttpResponseMessage PrepareCachedEntry(this HttpRequestMessage request, CacheData cachedData)
    {
        var response = cachedData.CachableResponse;
        if (cachedData.Headers != null)
        {
            foreach (var kvp in cachedData.Headers)
            {
                response.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }

        response.Content = new ByteArrayContent(cachedData.Data);
        if (cachedData.ContentHeaders != null)
        {
            foreach (var kvp in cachedData.ContentHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }
        }

        response.RequestMessage = request;
        return response;
    }
}
