using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ThePornDB.Helpers;
using ThePornDB.Models;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
#else
using System.Net.Http;
using MediaBrowser.Controller.Entities.Movies;
#endif

namespace ThePornDB.Providers
{
    public class CollectionsImages : IRemoteImageProvider
    {
        public string Name => Plugin.Instance.Name;

        public bool Supports(BaseItem item) => item is BoxSet;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
            => new List<ImageType>
            {
                ImageType.Primary,
                ImageType.Logo,
                ImageType.Disc,
            };

#if __EMBY__
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
#else
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
#endif
        {
            List<RemoteImageInfo> images = new List<RemoteImageInfo>();

            if (item == null || !item.ProviderIds.TryGetValue(this.Name, out var curID))
            {
                return images;
            }

            Site? siteData = null;
            try
            {
                siteData = await MetadataAPI.SiteUpdate(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetImages error: \"{e}\"");
            }

            var imgs = new Dictionary<ImageType, string>();
            if (siteData.HasValue)
            {
                imgs.Add(ImageType.Primary, siteData.Value.Poster);
                imgs.Add(ImageType.Logo, siteData.Value.Logo);
                imgs.Add(ImageType.Disc, siteData.Value.Favicon);
            }

            foreach (var image in imgs)
            {
                if (!string.IsNullOrEmpty(image.Value))
                {
                    var res = new RemoteImageInfo
                    {
                        ProviderName = Plugin.Instance.Name,
                        Url = image.Value,
                        Type = image.Key,
                    };

                    images.Add(res);
                }
            }

            return images;
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
