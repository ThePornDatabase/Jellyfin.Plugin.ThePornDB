using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
#else
using System.Net.Http;
using Microsoft.Extensions.Logging;
#endif

namespace ThePornDB.Providers
{
    public class Movies : IRemoteMetadataProvider<Movie, MovieInfo>
    {
#if __EMBY__
        public Movies(ILogManager logger, IHttpClient http)
        {
            if (logger != null)
            {
                Log = logger.GetLogger(this.Name);
            }

            Http = http;
        }

        public static IHttpClient Http { get; set; }
#else
        public Movies(ILogger<Movies> logger, IHttpClientFactory http)
        {
            Log = logger;
            Http = http;
        }

        public static IHttpClientFactory Http { get; set; }
#endif

        public static ILogger Log { get; set; }

        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null || string.IsNullOrEmpty(searchInfo.Name) || string.IsNullOrEmpty(Plugin.Instance.Configuration.MetadataAPIToken))
            {
                return result;
            }

            try
            {
                result = await MetadataAPI.SceneSearch(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (result.Any())
            {
                foreach (var scene in result)
                {
                    if (scene.PremiereDate.HasValue)
                    {
                        scene.ProductionYear = scene.PremiereDate.Value.Year;
                    }
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                HasMetadata = false,
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (info == null)
            {
                return result;
            }

            var sceneID = info.ProviderIds;
            sceneID.TryGetValue(this.Name, out var curID);

            if (!sceneID.ContainsKey(this.Name) || curID == null)
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    var first = searchResults.First();

                    sceneID = first.ProviderIds;

                    sceneID.TryGetValue(this.Name, out curID);
                }
            }

            if (curID == null)
            {
                return result;
            }

            try
            {
                result = await MetadataAPI.SceneUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (!string.IsNullOrEmpty(result.Item.Name))
            {
                result.HasMetadata = true;
                result.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                result.Item.OfficialRating = "XXX";

                if (result.Item.PremiereDate.HasValue)
                {
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }

                foreach (var actorLink in result.People)
                {
                    actorLink.Type = PersonType.Actor;
                }
            }

            return result;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Http.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                EnableDefaultUserAgent = false,
                UserAgent = Consts.UserAgent,
            });
        }
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", Consts.UserAgent);

            return Http.CreateClient().SendAsync(request, cancellationToken);
        }
#endif
    }
}
