using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
#endif

namespace ThePornDB.Providers
{
    public class Scenes : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        private static readonly SceneType ProviderSceneType = SceneType.Scene;

        private readonly IEnumerable<SceneType> otherTypes = Enum.GetValues(typeof(SceneType)).Cast<SceneType>().Where(o => o != ProviderSceneType);

        public string Name => Plugin.Instance.Name + " Scenes";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = (List<RemoteSearchResult>)await Base.GetSearchResults(searchInfo, SceneType.Scene, cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                HasMetadata = true,
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            foreach (var item in this.otherTypes)
            {
                var (providerIdName, _, _) = Base.GetSettings(item);
                info.ProviderIds.TryGetValue(providerIdName, out var curID);
                if (!string.IsNullOrEmpty(curID))
                {
                    return result;
                }
            }

            result = await Base.GetMetadata(info, ProviderSceneType, cancellationToken).ConfigureAwait(false);

            return result;
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return UGetImageResponse.SendAsync(url, cancellationToken);
        }
    }
}
