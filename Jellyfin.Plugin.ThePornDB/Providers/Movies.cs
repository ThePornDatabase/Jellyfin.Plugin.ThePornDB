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
using ThePornDB.Helpers;

#if __EMBY__
using MediaBrowser.Common.Net;
#else
using System.Net.Http;
using ThePornDB.Helpers.Utils;
#endif

namespace ThePornDB.Providers
{
    public class Movies : IRemoteMetadataProvider<Movie, MovieInfo>
    {
        public string Name => Plugin.Instance.Name;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();

            if (searchInfo == null || string.IsNullOrEmpty(Plugin.Instance.Configuration.MetadataAPIToken))
            {
                return result;
            }

            if (searchInfo.ProviderIds.TryGetValue(this.Name, out var curID) && !string.IsNullOrEmpty(curID))
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
                        ProviderIds = { { Plugin.Instance.Name, curID } },
                        Name = sceneData.Item.Name,
                        ImageUrl = sceneImages?.Where(o => o.Type == ImageType.Primary).FirstOrDefault()?.Url,
                        PremiereDate = sceneData.Item.PremiereDate,
                    });
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
                result = await MetadataAPI.SceneSearch(searchTitle, oshash, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error($"Search error: \"{e}\"");
            }

            if (result.Any())
            {
                foreach (var scene in result)
                {
                    if (scene.PremiereDate.HasValue)
                    {
                        scene.ProductionYear = scene.PremiereDate.Value.Year;
                    }
                }
            }

            return result;
        }

        public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                HasMetadata = false,
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (info == null)
            {
                return result;
            }

            info.ProviderIds.TryGetValue(this.Name, out var curID);

            if (string.IsNullOrEmpty(curID))
            {
                var searchResults = await this.GetSearchResults(info, cancellationToken).ConfigureAwait(false);
                if (searchResults.Any())
                {
                    searchResults.First().ProviderIds.TryGetValue(this.Name, out curID);
                }
            }

            if (string.IsNullOrEmpty(curID))
            {
                return result;
            }

            try
            {
                result = await MetadataAPI.SceneUpdate(curID, cancellationToken).ConfigureAwait(false);
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

                if (result.Item.Studios.Any())
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

                if (result.Item.Genres.Any())
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

                    result.Item.Genres = genres.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(o => o).ToArray();
                }

                if (result.People.Any())
                {
                    foreach (var actorLink in result.People)
                    {
                        actorLink.Type = PersonType.Actor;
                    }

                    var people = result.People.Where(o => o.ProviderIds.ContainsKey(this.Name) && !string.IsNullOrEmpty(o.ProviderIds[this.Name]));
                    var other = result.People.Where(o => !o.ProviderIds.ContainsKey(this.Name) || string.IsNullOrEmpty(o.ProviderIds[this.Name]));

                    result.People = people
                        .DistinctBy(o => o.ProviderIds[this.Name], StringComparer.OrdinalIgnoreCase)
                        .OrderBy(o => string.IsNullOrEmpty(o.Role))
                        .ThenBy(o => o.Role?.Equals("Male", StringComparison.OrdinalIgnoreCase))
                        .ThenBy(o => o.Name)
                        .ToList();
                    result.People.AddRange(other.OrderBy(o => o.Name));
                }

                if (Plugin.Instance.Configuration.UseCustomTitle)
                {
                    var parameters = new Dictionary<string, object>()
                    {
                        { "{title}", result.Item.Name },
                        { "{studio}", result.Item.Studios.First() },
                        { "{studios}", string.Join(", ", result.Item.Studios) },
                        { "{actors}", string.Join(", ", result.People.Select(o => o.Name)) },
                        { "{release_date}", result.Item.PremiereDate.HasValue ? result.Item.PremiereDate.Value.DateTime.ToString("yyyy-MM-dd") : string.Empty},
                    };

                    result.Item.Name = parameters.Aggregate(Plugin.Instance.Configuration.CustomTitle, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
                }
            }

            return result;
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
