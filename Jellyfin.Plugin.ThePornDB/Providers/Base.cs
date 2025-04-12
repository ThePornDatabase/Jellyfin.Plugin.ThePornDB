using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using ThePornDB.Configuration;
using ThePornDB.Helpers;
using ThePornDB.Helpers.Utils;

namespace ThePornDB.Providers
{
    public enum SceneType
    {
        Scene = 0,
        Movie = 1,
        JAV = 2,
    }

    public static class Base
    {
        private static readonly char[] Separator = new[] { ' ' };

        public static (string prefixID, string searchURL, string sceneURL) GetSettings(SceneType sceneType)
        {
            string prefixID = string.Empty,
                searchURL = string.Empty,
                sceneURL = string.Empty;

            switch (sceneType)
            {
                case SceneType.Scene:
                    prefixID = "scenes/";
                    searchURL = Consts.APISceneSearchURL;
                    sceneURL = Consts.APISceneURL;
                    break;
                case SceneType.Movie:
                    prefixID = "movies/";
                    searchURL = Consts.APIMovieSearchURL;
                    sceneURL = Consts.APIMovieURL;
                    break;
                case SceneType.JAV:
                    prefixID = "jav/";
                    searchURL = Consts.APIJAVSearchURL;
                    sceneURL = Consts.APIJAVURL;
                    break;
            }

            return (prefixID, searchURL, sceneURL);
        }

        public static async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, SceneType sceneType, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null || string.IsNullOrEmpty(Plugin.Instance.Configuration.MetadataAPIToken))
            {
                return result;
            }

            var (prefixID, searchURL, sceneURL) = GetSettings(sceneType);

            var curID = searchInfo.Name.GetAttributeValue("theporndbid") ?? searchInfo.Name.GetAttributeValue("TPDBID");
            if (string.IsNullOrEmpty(curID))
            {
                searchInfo.ProviderIds.TryGetValue(Plugin.Instance.Name, out curID);
            }

            if (!string.IsNullOrEmpty(curID))
            {
                var sceneData = new MetadataResult<Movie>()
                {
                    HasMetadata = false,
                    Item = new Movie(),
                    People = new List<PersonInfo>(),
                };

                var sceneImages = new List<RemoteImageInfo>();

                try
                {
                    sceneData = await MetadataAPI.SceneUpdate(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"Update error: \"{e}\"");
                }

                try
                {
                    sceneImages = (List<RemoteImageInfo>)await MetadataAPI.SceneImages(curID, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Error($"GetImages error: \"{e}\"");
                }

                if (sceneData.HasMetadata)
                {
                    result.Add(new RemoteSearchResult
                    {
                        ProviderIds = { { Plugin.Instance.Name, curID.StartsWith(prefixID, StringComparison.OrdinalIgnoreCase) ? curID : prefixID + curID } },
                        Name = sceneData.Item.Name,
                        ImageUrl = sceneImages?.Where(o => o.Type == ImageType.Primary).FirstOrDefault()?.Url,
                        PremiereDate = sceneData.Item.PremiereDate,
                    });

                    return result;
                }
            }

            if (string.IsNullOrEmpty(searchInfo.Name))
            {
                return result;
            }

            string searchTitle = searchInfo.Name,
                oshash = string.Empty;
#if __EMBY__
#else
            if (!string.IsNullOrEmpty(searchInfo.Path) && Plugin.Instance.Configuration.UseFilePath)
            {
                searchTitle = searchInfo.Path;
            }

            if (!string.IsNullOrEmpty(searchInfo.Path) && Plugin.Instance.Configuration.UseOSHash)
            {
                oshash = OpenSubtitlesHash.ComputeHash(searchInfo.Path);
            }
#endif

            try
            {
                result = await MetadataAPI.SceneSearch(searchTitle, oshash, searchInfo.Year, searchURL, prefixID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (result.Count > 0)
            {
                foreach (var scene in result)
                {
                    if (scene.PremiereDate.HasValue)
                    {
                        scene.ProductionYear = scene.PremiereDate.Value.Year;
                    }
                }

                switch (Plugin.Instance.Configuration.OrderStyle)
                {
                    case OrderStyle.Default:
                        break;
                    case OrderStyle.DistanceByTitle:
                        result = result.OrderByDescending(o => 100 - LevenshteinDistance.Calculate(searchTitle, o.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                        break;
                }
            }

            return result;
        }

        public static async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, SceneType sceneType, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                HasMetadata = true,
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (Plugin.Instance.Configuration.UseUnmatchedTag && !string.IsNullOrEmpty(Plugin.Instance.Configuration.UnmatchedTag))
            {
                result.Item.Genres = new string[] { Plugin.Instance.Configuration.UnmatchedTag };
            }

            if (info == null)
            {
                return result;
            }

            var (prefixID, searchURL, sceneURL) = GetSettings(sceneType);

            info.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID);
            if (string.IsNullOrEmpty(curID) && !Plugin.Instance.Configuration.DisableMediaAutoIdentify)
            {
                var searchResults = await GetSearchResults(info, sceneType, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    searchResults.First().ProviderIds.TryGetValue(Plugin.Instance.Name, out curID);
                }
            }

            if (string.IsNullOrEmpty(curID))
            {
                return result;
            }

            result.HasMetadata = false;
            try
            {
                result = await MetadataAPI.SceneUpdate(curID, cancellationToken, Plugin.Instance.Configuration.AddCollectionOnSite).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Update error: \"{e}\"");
            }

            if (result.HasMetadata)
            {
                result.Item.ProviderIds.Add(Plugin.Instance.Name, curID);
                result.Item.OfficialRating = "XXX";

                if (result.Item.PremiereDate.HasValue)
                {
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }

                if (result.Item.Studios.Length > 0)
                {
                    var studios = new List<string>();

                    foreach (var studioLink in result.Item.Studios)
                    {
                        var studioName = studioLink;
                        if (studioLink.ToLower().Equals(studioLink, StringComparison.Ordinal))
                        {
                            studioName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(studioLink);
                        }

                        studios.Add(studioName);
                    }

                    result.Item.Studios = studios.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                }

                var tags = new List<string>();
                if (result.Item.Genres.Length > 0)
                {
                    var genres = new List<string>();

                    foreach (var genreLink in result.Item.Genres)
                    {
                        var genreName = genreLink;
                        if (genreLink.ToLower().Equals(genreLink, StringComparison.Ordinal))
                        {
                            genreName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(genreLink);
                        }

                        genres.Add(genreName);
                    }

                    tags = genres.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(o => o).ToList();
                }

                switch (Plugin.Instance.Configuration.TagStyle)
                {
                    case TagStyle.Disabled:
                        result.Item.Genres = Array.Empty<string>();
                        result.Item.Tags = Array.Empty<string>();
                        break;
                    case TagStyle.Genre:
                        result.Item.Genres = tags.ToArray();
                        result.Item.Tags = Array.Empty<string>();
                        break;
                    case TagStyle.Tag:
                        result.Item.Genres = Array.Empty<string>();
                        result.Item.Tags = tags.ToArray();
                        break;
                }

                if (result.People.Count > 0)
                {
                    var people = result.People.Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name) && !string.IsNullOrEmpty(o.ProviderIds[Plugin.Instance.Name]));
                    var other = result.People.Where(o => !o.ProviderIds.ContainsKey(Plugin.Instance.Name) || string.IsNullOrEmpty(o.ProviderIds[Plugin.Instance.Name]));

                    result.People = people
                        .DistinctBy(o => o.ProviderIds[Plugin.Instance.Name], StringComparer.OrdinalIgnoreCase)
                        .OrderBy(o => o.Type)
                        .ThenBy(o => string.IsNullOrEmpty(o.Role))
                        .ThenBy(o => o.Role?.Equals("Male", StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Name)
                        .ToList();
                    result.People.AddRange(other.OrderBy(o => o.Name));
                }

                if (Plugin.Instance.Configuration.UseCustomTitle && !string.IsNullOrEmpty(Plugin.Instance.Configuration.CustomTitle))
                {
                    var parameters = new Dictionary<string, object>()
                    {
                        { "{title}", result.Item.Name },
                        { "{studio}", result.Item.Studios.First() },
                        { "{studios}", string.Join(", ", result.Item.Studios) },
                        { "{actors}", string.Join(", ", result.People.Select(o => o.Name)) },
#if __EMBY__
                        { "{release_date}", result.Item.PremiereDate.HasValue ? result.Item.PremiereDate.Value.DateTime.ToString("yyyy-MM-dd") : string.Empty },
#else
                        { "{release_date}", result.Item.PremiereDate.HasValue ? result.Item.PremiereDate.Value.ToString("yyyy-MM-dd") : string.Empty },
#endif
                    };

                    result.Item.Name = parameters.Aggregate(Plugin.Instance.Configuration.CustomTitle, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
                    result.Item.Name = string.Join(" ", result.Item.Name.Split(Separator, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else
            {
                result.HasMetadata = true;
            }

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            IEnumerable<RemoteImageInfo> images = new List<RemoteImageInfo>();

            if (item == null || !item.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID))
            {
                return images;
            }

            try
            {
                images = await MetadataAPI.SceneImages(curID, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"GetImages error: \"{e}\"");
            }

            if (images.Any())
            {
                foreach (var image in images)
                {
                    image.ProviderName = Plugin.Instance.Name;
                }
            }

            return images;
        }
    }
}
