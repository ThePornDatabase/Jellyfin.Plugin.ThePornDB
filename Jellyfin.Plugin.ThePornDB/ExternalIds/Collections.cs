using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
using MediaBrowser.Controller.Entities;
#else
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Providers;
#endif

namespace ThePornDB.ExternalIds
{
    public class Collections : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;

        public ExternalIdMediaType? Type => ExternalIdMediaType.BoxSet;
#endif

        public string Key => Plugin.Instance.Name;

        public string UrlFormatString => Consts.SiteURL;

        public bool Supports(IHasProviderIds item) => item is BoxSet;
    }
}
