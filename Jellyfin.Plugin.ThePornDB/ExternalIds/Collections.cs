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
#if __EMBY__
    public class Collections : IExternalId, IHasWebsite
#else
    public class Collections : IExternalId
#endif
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;

        public ExternalIdMediaType? Type => ExternalIdMediaType.BoxSet;
#endif

        public string Key => Plugin.Instance.Name;

#if __EMBY__
        public string UrlFormatString => Consts.SiteURL;

        public string Website => Consts.BaseURL;
#endif

        public bool Supports(IHasProviderIds item) => item is BoxSet;
    }
}
