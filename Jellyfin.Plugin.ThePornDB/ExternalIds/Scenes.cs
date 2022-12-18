using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace ThePornDB
{
    public class Scenes : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name + " Scene";
#else
        public string ProviderName => Plugin.Instance.Name + " Scene";

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;
#endif

        public string Key => Plugin.Instance.Name;

        public string UrlFormatString => Consts.SceneURL;

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
