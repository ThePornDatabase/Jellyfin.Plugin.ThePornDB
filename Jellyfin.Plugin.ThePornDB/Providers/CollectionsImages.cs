using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
#else
using System.Net.Http;
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
            IList<RemoteImageInfo> images = new List<RemoteImageInfo>();

            if (item == null || !item.ProviderIds.TryGetValue(this.Name, out var curID))
            {
                return images;
            }

            JObject siteData = null;
            try
            {
                siteData = await MetadataAPI.SiteUpdate(int.Parse(curID), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetImages error: \"{e}\"");
            }

            var imgs = new Dictionary<ImageType, string>();
            if (siteData != null)
            {
                imgs.Add(ImageType.Primary, (string)siteData["poster"]);
                imgs.Add(ImageType.Logo, (string)siteData["logo"]);
                imgs.Add(ImageType.Disc, (string)siteData["favicon"]);
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
