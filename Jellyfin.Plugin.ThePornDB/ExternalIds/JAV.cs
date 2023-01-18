using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace ThePornDB
{
    public class JAV : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name + " JAV";
#else
        public string ProviderName => Plugin.Instance.Name + " JAV";

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;
#endif

        public string Key => Plugin.Instance.Name + "JAV";

        public string UrlFormatString => Consts.JAVURL;

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
