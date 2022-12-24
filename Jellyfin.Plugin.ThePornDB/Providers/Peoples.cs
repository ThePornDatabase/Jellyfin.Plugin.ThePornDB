using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
#endif

namespace ThePornDB.Providers
{
    public class Peoples : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null)
            {
                return result;
            }

            if (searchInfo.ProviderIds.TryGetValue(this.Name, out var curID) && !string.IsNullOrEmpty(curID))
            {
                var actorData = new MetadataResult<Person>()
                {
                    HasMetadata = false,
                    Item = new Person(),
                };

                IEnumerable<RemoteImageInfo> sceneImages = new List<RemoteImageInfo>();

                try
                {
                    actorData = await MetadataAPI.PeopleUpdate(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"Update error: \"{e}\"");
                }

                try
                {
                    sceneImages = await MetadataAPI.PeopleImages(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"GetImages error: \"{e}\"");
                }

                if (actorData.HasMetadata)
                {
                    result.Add(new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = actorData.Item.ExternalId,
                        ImageUrl = sceneImages?.Where(o => o.Type == ImageType.Primary).FirstOrDefault()?.Url,
                        PremiereDate = actorData.Item.PremiereDate,
                    });
                }
            }

            if (searchInfo.ProviderIds.Any(o => !string.IsNullOrEmpty(o.Value)))
            {
                return result;
            }

            try
            {
                result = await MetadataAPI.PeopleSearch(searchInfo.Name, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Actor Search error: \"{e}\"");
            }

            return result;
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                HasMetadata = false,
                Item = new Person(),
            };

            if (info == null)
            {
                return result;
            }

            info.ProviderIds.TryGetValue(this.Name, out var curID);
            if (string.IsNullOrEmpty(curID) && !Plugin.Instance.Configuration.DisableActorsAutoIdentify)
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

            try
            {
                result = await MetadataAPI.PeopleUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Actor Update error: \"{e}\"");
            }

            if (result.HasMetadata)
            {
                result.Item.ProviderIds.Add(Plugin.Instance.Name, curID);

                if (result.Item.PremiereDate.HasValue)
                {
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }
            }

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
