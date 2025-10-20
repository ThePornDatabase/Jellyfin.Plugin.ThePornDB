#if __EMBY__
#else
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace ThePornDB.ExternalIds
{
    public class ExternalUrlProvider : IExternalUrlProvider
    {
        public string Name => Plugin.Instance.Name;

        public IEnumerable<string> GetExternalUrls(BaseItem item)
        {
            if (item.TryGetProviderId(this.Name, out var externalId))
            {
                if (item is Person)
                {
                    yield return string.Format(Consts.PerfomerURL, externalId);
                }
                else if (item is BoxSet)
                {
                    yield return string.Format(Consts.SiteURL, externalId);
                }
                else
                {
                    yield return string.Format(Consts.BaseURL + "/{0}", externalId);
                }
            }
        }
    }
}
#endif
