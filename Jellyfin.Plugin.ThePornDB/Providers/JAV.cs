using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
#if __EMBY__
    public class JAV : IRemoteMetadataProvider<Movie, MovieInfo>, IHasSupportedExternalIdentifiers
#else
    public class JAV : IRemoteMetadataProvider<Movie, MovieInfo>
#endif
    {
        private static readonly SceneType ProviderSceneType = SceneType.JAV;

        public string Name => Plugin.Instance.Name + " JAV";

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = (List<RemoteSearchResult>)await Base.GetSearchResults(searchInfo, SceneType.JAV, cancellationToken).ConfigureAwait(false);

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return await Base.GetMetadata(info, ProviderSceneType, cancellationToken).ConfigureAwait(false);
        }

#if __EMBY__
        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
#else
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
#endif
        {
            return UGetImageResponse.SendAsync(url, cancellationToken);
        }

#if __EMBY__
        public string[] GetSupportedExternalIdentifiers()
        {
            return new[] { Plugin.Instance.Name };
        }
#endif
    }
}
