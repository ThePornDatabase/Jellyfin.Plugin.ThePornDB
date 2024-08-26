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
using ThePornDB.Configuration;
using ThePornDB.Helpers;
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
                headers.Add("Accept", "application/json");
            }

            var http = await HTTP.Request(url, cancellationToken, headers).ConfigureAwait(false);
            try
            {
                json = JObject.Parse(http.Content);
            }
            catch (Exception)
            {
                var message = "Unknown error";
                if (json != null && json.ContainsKey("message"))
                {
                    message = (string)json["message"];
                }

                Logger.Error($"API error: \"{message}\"");
            }

            return json;
        }

        public static async Task<List<RemoteSearchResult>> SceneSearch(string searchTitle, string oshash, int? year, string url, string prefixID, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            url = string.Format(url, Uri.EscapeDataString(searchTitle), Uri.EscapeDataString(oshash), Uri.EscapeDataString(year.HasValue ? year.Value.ToString() : string.Empty));
            var data = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (data == null || !data.ContainsKey("data") || data["data"].Type != JTokenType.Array)
            {
                return result;
            }

            foreach (var searchResult in data["data"])
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, prefixID + (string)searchResult["id"] } },
                    Name = (string)searchResult["title"],
                    ImageUrl = (string)searchResult["poster"],
                    PremiereDate = (DateTime)searchResult["date"],
                });
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

            var url = Consts.APIBaseURL + "/" + sceneID;
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            result.Item.Name = (string)sceneData["title"];
            result.Item.Overview = (string)sceneData["description"];

            if (sceneData.ContainsKey("site") && sceneData["site"].Type == JTokenType.Object)
            {
                if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Site)
                {
                    result.Item.AddStudio((string)sceneData["site"]["name"]);
                }

                if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Parent)
                {
                    int? site_id = (int)sceneData["site"]["id"],
                        parent_id = (int?)sceneData["site"]["parent_id"];

                    if (parent_id.HasValue && !site_id.Equals(parent_id))
                    {
                        if (sceneData["site"]["parent"]?.Type == JTokenType.Object)
                        {
                            result.Item.AddStudio((string)sceneData["site"]["parent"]["name"]);
                        }
                        else
                        {
                            var siteData = await SiteUpdate(parent_id.Value.ToString(), cancellationToken).ConfigureAwait(false);
                            if (siteData != null)
                            {
                                result.Item.AddStudio((string)siteData["name"]);
                            }
                        }
                    }
                }

                if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Network)
                {
                    int? site_id = (int)sceneData["site"]["id"],
                        network_id = (int?)sceneData["site"]["network_id"];

                    if (network_id.HasValue && !site_id.Equals(network_id))
                    {
                        if (sceneData["site"]["network"]?.Type == JTokenType.Object)
                        {
                            result.Item.AddStudio((string)sceneData["site"]["network"]["name"]);
                        }
                        else
                        {
                            var siteData = await SiteUpdate(network_id.Value.ToString(), cancellationToken).ConfigureAwait(false);
                            if (siteData != null)
                            {
                                result.Item.AddStudio((string)siteData["name"]);
                            }
                        }
                    }
                }
            }

            var trailer = (string)sceneData["trailer"];
            if (!string.IsNullOrEmpty(trailer))
            {
                result.Item.AddTrailerUrl((string)sceneData["trailer"]);
            }

            result.Item.PremiereDate = (DateTime)sceneData["date"];

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
                        name = (string)actorLink["name"],
                        gender = string.Empty,
                        role = string.Empty,
                        face = (string)actorLink["face"],
                        image = (string)actorLink["image"];

                    if (actorLink["parent"] != null && actorLink["parent"].Type == JTokenType.Object)
                    {
                        if (actorLink["parent"]["id"] != null)
                        {
                            curID = (string)actorLink["parent"]["id"];
                        }

                        if (actorLink["parent"]["name"] != null)
                        {
                            name = (string)actorLink["parent"]["name"];

                            if (actorLink["parent"]["disambiguation"] != null && !string.IsNullOrEmpty((string)actorLink["parent"]["disambiguation"]))
                            {
                                name += " (" + (string)actorLink["parent"]["disambiguation"] + ")";
                            }
                        }

                        if (actorLink["parent"]["extras"]["gender"] != null)
                        {
                            gender = (string)actorLink["parent"]["extras"]["gender"];
                        }

                        if (actorLink["parent"]["face"] != null)
                        {
                            face = (string)actorLink["parent"]["face"];
                        }

                        if (actorLink["parent"]["image"] != null)
                        {
                            image = (string)actorLink["parent"]["image"];
                        }
                    }

                    var actor = new PersonInfo
                    {
                        Name = name,
                    };

                    switch (Plugin.Instance.Configuration.ActorsImage)
                    {
                        case ActorsImageStyle.Face:
                            actor.ImageUrl = face;
                            break;
                        case ActorsImageStyle.Poster:
                            actor.ImageUrl = image;
                            break;
                    }

                    if (!string.IsNullOrEmpty(curID))
                    {
                        actor.ProviderIds.Add(Plugin.Instance.Name, curID);
                    }

                    switch (Plugin.Instance.Configuration.ActorsRole)
                    {
                        case ActorsRoleStyle.Gender:
                            role = gender;
                            break;
                        case ActorsRoleStyle.NameByScene:
                            role = (string)actorLink["name"];
                            break;
                        case ActorsRoleStyle.None:
                            role = string.Empty;
                            break;
                    }

                    if (!string.IsNullOrEmpty(role))
                    {
                        actor.Role = role;
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

            var url = Consts.APIBaseURL + "/" + sceneID;
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            var images = new List<(ImageType Type, string Url)>
            {
                (ImageType.Backdrop, (string)sceneData["background"]["large"]),
            };

            JToken primary = null;
            switch (Plugin.Instance.Configuration.ScenesImage)
            {
                case ScenesImageStyle.Poster:
                    primary = sceneData["posters"]["large"];
                    break;
                case ScenesImageStyle.Background:
                    primary = sceneData["background"]["large"];
                    break;
            }

            if (primary != null)
            {
                images.Insert(0, (ImageType.Primary, (string)primary));
            }

            foreach (var image in images)
            {
                if (string.IsNullOrEmpty(image.Url))
                {
                    continue;
                }

                var res = new RemoteImageInfo
                {
                    Url = image.Url,
                    Type = image.Type,
                };

                var reg = RegExImageSize.Match(image.Url);
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

            var url = string.Format(Consts.APIPerfomerSearchURL, Uri.EscapeDataString(actorName));
            var data = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);

            if (data == null || !data.ContainsKey("data") || data["data"].Type != JTokenType.Array)
            {
                return result;
            }

            foreach (var searchResult in data["data"])
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, (string)searchResult["id"] } },
                    Name = (searchResult["disambiguation"] != null && !string.IsNullOrEmpty((string)searchResult["disambiguation"])) ? (string)searchResult["name"] + " (" + (string)searchResult["disambiguation"] + ")" : (string)searchResult["name"],
                    ImageUrl = (string)searchResult["image"],
                });
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

            var url = string.Format(Consts.APIPerfomerURL, Uri.EscapeDataString(sceneID));
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            // result.Item.Name = (string)sceneData["name"];
            result.Item.ExternalId = (sceneData["disambiguation"] != null && !string.IsNullOrEmpty((string)sceneData["disambiguation"])) ? (string)sceneData["name"] + " (" + (string)sceneData["disambiguation"] + ")" : (string)sceneData["name"];
            result.Item.OriginalTitle = string.Join(", ", sceneData["aliases"].Select(o => o.ToString().Trim()));
            result.Item.Overview = ActorsOverview.CustomFormat(sceneData);

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

            var url = string.Format(Consts.APIPerfomerURL, Uri.EscapeDataString(sceneID));
            var sceneData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (sceneData == null || !sceneData.ContainsKey("data") || sceneData["data"].Type != JTokenType.Object)
            {
                return result;
            }

            sceneData = (JObject)sceneData["data"];

            if (Plugin.Instance.Configuration.ActorsImage == ActorsImageStyle.Face)
            {
                var posterURL = (string)sceneData["face"];
                if (!string.IsNullOrEmpty(posterURL))
                {
                    var res = GetRemoteImageFromURL(posterURL);

                    result.Add(res);
                }
            }

            foreach (var poster in sceneData["posters"])
            {
                var posterURL = (string)poster["url"];
                if (!string.IsNullOrEmpty(posterURL))
                {
                    var res = GetRemoteImageFromURL(posterURL);

                    result.Add(res);
                }
            }

            return result;
        }

        public static async Task<JArray> SiteSearch(string name, CancellationToken cancellationToken)
        {
            JArray result = null;

            var url = string.Format(Consts.APISiteSearchURL, Uri.EscapeDataString(name));
            var siteData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (siteData != null && siteData.ContainsKey("data") && siteData["data"].Type == JTokenType.Array)
            {
                result = (JArray)siteData["data"];
            }

            return result;
        }

        public static async Task<JObject> SiteUpdate(string id, CancellationToken cancellationToken)
        {
            JObject result = null;

            var url = string.Format(Consts.APISiteURL, Uri.EscapeDataString(id));
            var siteData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (siteData != null && siteData.ContainsKey("data") && siteData["data"].Type == JTokenType.Object)
            {
                result = (JObject)siteData["data"];
            }

            return result;
        }

        private static RemoteImageInfo GetRemoteImageFromURL(string url)
        {
            var res = new RemoteImageInfo
            {
                Url = url,
                Type = ImageType.Primary,
            };

            var reg = RegExImageSize.Match(url);
            if (reg.Success)
            {
                res.Width = int.Parse(reg.Groups["Width"].Value);
                res.Height = int.Parse(reg.Groups["Height"].Value);
            }

            return res;
        }
    }
}
