using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Model.Providers;
#endif

namespace ThePornDB
{
    public class Movies : IExternalId
    {
#if __EMBY__
        public string Name => Plugin.Instance.Name + " Movie";
#else
        public string ProviderName => Plugin.Instance.Name + " Movie";

        public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;
#endif

        public string Key => Plugin.Instance.Name + "Movie";

        public string UrlFormatString => Consts.MovieURL;

        public bool Supports(IHasProviderIds item) => item is Movie;
    }
}
