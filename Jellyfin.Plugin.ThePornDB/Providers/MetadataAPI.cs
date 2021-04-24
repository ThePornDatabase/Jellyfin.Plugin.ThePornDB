using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json.Linq;
using ThePornDB.Helpers.Utils;

namespace ThePornDB.Providers
{
    public static class MetadataAPI
    {
        private static readonly Regex RegExImageSize = new Regex(@"(?<Width>[0-9]{1,})x(?<Height>[0-9]{1,})", RegexOptions.Compiled);

        public static async Task<JObject> GetDataFromAPI(string url, CancellationToken cancellationToken)
        {
            JObject json = null;
            var headers = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(Plugin.Instance.Configuration.MetadataAPIToken))
            {
                headers.Add("Authorization", $"Bearer {Plugin.Instance.Configuration.MetadataAPIToken}");
            }

            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
            if (http.IsOK)
            {
                json = JObject.Parse(http.Content);
            }

            return json;
        }

        public static async Task<List<RemoteSearchResult>> SceneSearch(string searchTitle, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var url = string.Format(Consts.APISceneSearchURL, searchTitle);
            var data = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);

            if (data == null || !data.ContainsKey("data") || data["data"].Type != JTokenType.Array)
            {
                return result;
            }

            foreach (var searchResult in data["data"])
            {
                string curID = (string)searchResult["_id"],
                    sceneName = (string)searchResult["title"],
                    sceneDate = (string)searchResult["date"],
                    scenePoster = (string)searchResult["poster"];

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    res.PremiereDate = sceneDateObj;
                }

                result.Add(res);
            }

            return result;
        }

        public static async Task<MetadataResult<Movie>> SceneUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Movie>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            var url = string.Format(Consts.APISceneURL, sceneID);
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];

            result.Item.AddStudio((string)sceneData["site"]["name"]);

            int siteID = (int)sceneData["site"]["id"],
                network_id = (int)sceneData["site"]["network_id"];

            if (!siteID.Equals(network_id))
            {
                url = string.Format(Consts.APISiteURL, network_id);
                var siteData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
                if (siteData != null && siteData.ContainsKey("data") && siteData["data"].Type == JTokenType.Object)
                {
                    siteData = (JObject)siteData["data"];

                    result.Item.AddStudio((string)siteData["name"]);
                }
            }

            var trailer = (string)sceneData["trailer"];
            if (!string.IsNullOrEmpty(trailer))
            {
                result.Item.AddTrailerUrl((string)sceneData["trailer"]);
            }

            var sceneDate = (string)sceneData["date"];
            if (DateTime.TryParseExact(sceneDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            if (sceneData.ContainsKey("tags"))
            {
                foreach (var genreLink in sceneData["tags"])
                {
                    var genreName = (string)genreLink["name"];

                    result.Item.AddGenre(genreName);
                }
            }

            if (sceneData.ContainsKey("performers"))
            {
                foreach (var actorLink in sceneData["performers"])
                {
                    string curID = string.Empty,
                        gender = string.Empty;
                    if (actorLink["parent"] != null && actorLink["parent"].Type == JTokenType.Object)
                    {
                        if (actorLink["parent"]["_id"] != null)
                        {
                            curID = (string)actorLink["parent"]["_id"];
                        }

                        if (actorLink["parent"]["extras"]["gender"] != null)
                        {
                            gender = (string)actorLink["parent"]["extras"]["gender"];
                        }
                    }

                    var actor = new PersonInfo
                    {
                        Name = (string)actorLink["name"],
                        ImageUrl = (string)actorLink["image"],
                    };

                    if (!string.IsNullOrEmpty(curID))
                    {
                        actor.ProviderIds.Add(Plugin.Instance.Name, (string)actorLink["parent"]["_id"]);
                    }

                    if (!string.IsNullOrEmpty(gender))
                    {
                        actor.Role = gender;
                    }

                    result.People.Add(actor);
                }
            }

            result.HasMetadata = true;

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> SceneImages(string sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var url = string.Format(Consts.APISceneURL, sceneID);
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            var images = new Dictionary<ImageType, string>
            {
                { ImageType.Primary, (string)sceneData["posters"]["large"] },
                { ImageType.Backdrop, (string)sceneData["background"]["large"] },
            };

            foreach (var image in images)
            {
                var res = new RemoteImageInfo
                {
                    Url = image.Value,
                    Type = image.Key,
                };

                var reg = RegExImageSize.Match(image.Value);
                if (reg.Success)
                {
                    res.Width = int.Parse(reg.Groups["Width"].Value);
                    res.Height = int.Parse(reg.Groups["Height"].Value);
                }

                result.Add(res);
            }

            return result;
        }

        public static async Task<List<RemoteSearchResult>> PeopleSearch(string actorName, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(actorName))
            {
                return result;
            }

            var url = string.Format(Consts.APIPerfomerSearchURL, actorName);
            var data = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);

            if (data == null || !data.ContainsKey("data") || data["data"].Type != JTokenType.Array)
            {
                return result;
            }

            foreach (var searchResult in data["data"])
            {
                string curID = (string)searchResult["_id"],
                    sceneName = (string)searchResult["name"],
                    scenePoster = (string)searchResult["image"];

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                result.Add(res);
            }

            return result;
        }

        public static async Task<MetadataResult<Person>> PeopleUpdate(string sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Person>()
            {
                Item = new Person(),
            };

            if (sceneID == null)
            {
                return result;
            }

            var url = string.Format(Consts.APIPerfomerURL, sceneID);
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            // result.Item.Name = (string)sceneData["name"];
            result.Item.OriginalTitle = string.Join(", ", sceneData["aliases"].Select(o => o.ToString().Trim()));
            result.Item.Overview = (string)sceneData["bio"];

            var actorBornDate = (string)sceneData["extras"]["birthday"];
            if (DateTime.TryParseExact(actorBornDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var actorBornPlace = (string)sceneData["extras"]["birthplace"];
            if (!string.IsNullOrEmpty(actorBornPlace))
            {
                result.Item.ProductionLocations = new string[] { actorBornPlace };
            }

            result.HasMetadata = true;

            return result;
        }

        public static async Task<IEnumerable<RemoteImageInfo>> PeopleImages(string sceneID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            var url = string.Format(Consts.APIPerfomerURL, sceneID);
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            foreach (var poster in sceneData["posters"])
            {
                var posterURL = (string)poster["url"];
                var res = new RemoteImageInfo
                {
                    Url = url,
                    Type = ImageType.Primary,
                };

                var reg = RegExImageSize.Match(posterURL);
                if (reg.Success)
                {
                    res.Width = int.Parse(reg.Groups["Width"].Value);
                    res.Height = int.Parse(reg.Groups["Height"].Value);
                }

                result.Add(res);
            }

            return result;
        }
    }
}
