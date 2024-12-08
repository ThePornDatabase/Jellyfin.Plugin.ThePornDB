using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
#else
using System.Net.Http;
using MediaBrowser.Controller.Entities.Movies;
#endif

namespace ThePornDB.Providers
{
#if __EMBY__
    public class Collections : IRemoteSearchProvider<BoxSetInfo>
#else
    public class Collections : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
#endif
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null || string.IsNullOrEmpty(Plugin.Instance.Configuration.MetadataAPIToken) || (Plugin.Instance.Configuration.UseUnmatchedTag && searchInfo.Name == Plugin.Instance.Configuration.UnmatchedTag))
            {
                return result;
            }

            var data = await MetadataAPI.SiteSearch(searchInfo.Name, cancellationToken).ConfigureAwait(false);

            if (data == null)
            {
                return result;
            }

            foreach (var searchResult in data)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, searchResult.UUID } },
                    Name = searchResult.Name,
                    ImageUrl = searchResult.Poster,
                });
            }

            return result;
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BoxSet>()
            {
                HasMetadata = false,
                Item = new BoxSet(),
            };

            if (info == null)
            {
                return result;
            }

            info.ProviderIds.TryGetValue(this.Name, out var curID);

            if (string.IsNullOrEmpty(curID))
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    searchResults.First().ProviderIds.TryGetValue(this.Name, out curID);
                }
            }

            if (string.IsNullOrEmpty(curID))
            {
                return result;
            }

            result.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
            result.HasMetadata = true;

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
