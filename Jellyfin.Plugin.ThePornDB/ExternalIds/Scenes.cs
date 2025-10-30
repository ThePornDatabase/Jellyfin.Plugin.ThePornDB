using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace ThePornDB.ExternalIds
{
#if __EMBY__
    public class Scenes : IExternalId, IHasWebsite
#else
    public class Scenes : IExternalId
#endif
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name;
#else
        public string ProviderName => Plugin.Instance.Name;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;
#endif

        public string Key => Plugin.Instance.Name;

#if __EMBY__
        public string UrlFormatString => Consts.BaseURL + "/{0}";

        public string Website => Consts.BaseURL;
#endif

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
