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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThePornDB.Configuration;
using ThePornDB.Helpers;
using ThePornDB.Helpers.Utils;
using ThePornDB.Models;

#if __EMBY__
#else
using Jellyfin.Data.Enums;
#endif

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
            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Array)
            {
                return result;
            }

            var data = http["data"].ToString();
            var searchResults = JsonConvert.DeserializeObject<List<Scene>>(data);

            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, prefixID + searchResult.UUID } },
                    Name = searchResult.Title,
                    ImageUrl = searchResult.Poster,
                    PremiereDate = searchResult.Date,
                });
            }

            return result;
        }

        public static async Task<MetadataResult<Movie>> SceneUpdate(string sceneID, CancellationToken cancellationToken, bool addCollectionOnSite = false)
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
            if (addCollectionOnSite)
            {
                url += "?add_to_collection=1";
            }

            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Object)
            {
                return result;
            }

            var data = http["data"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Scene>(data);

            result.Item.Name = sceneData.Title;
            result.Item.Overview = sceneData.Description;

            if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Site)
            {
                result.Item.AddStudio(sceneData.Site.Name);
            }

            if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Parent)
            {
                int? site_id = sceneData.Site.ID,
                    parent_id = sceneData.Site.ParentID;

                if (parent_id.HasValue && !site_id.Equals(parent_id))
                {
                    if (sceneData.Site.Parent.HasValue)
                    {
                        result.Item.AddStudio((string)sceneData.Site.Parent.Value.Name);
                    }
                    else
                    {
                        var siteData = await SiteUpdate(parent_id.Value.ToString(), cancellationToken).ConfigureAwait(false);
                        if (siteData.HasValue)
                        {
                            result.Item.AddStudio(siteData.Value.Name);
                        }
                    }
                }
            }

            if (Plugin.Instance.Configuration.StudioStyle == StudioStyle.All || Plugin.Instance.Configuration.StudioStyle == StudioStyle.Network)
            {
                int? site_id = sceneData.Site.ID,
                    network_id = sceneData.Site.NetworkID;

                if (network_id.HasValue && !site_id.Equals(network_id))
                {
                    if (sceneData.Site.Network.HasValue)
                    {
                        result.Item.AddStudio(sceneData.Site.Network.Value.Name);
                    }
                    else
                    {
                        var siteData = await SiteUpdate(network_id.Value.ToString(), cancellationToken).ConfigureAwait(false);
                        if (siteData.HasValue)
                        {
                            result.Item.AddStudio(siteData.Value.Name);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(sceneData.Trailer))
            {
                result.Item.AddTrailerUrl(sceneData.Trailer);
            }

            result.Item.PremiereDate = sceneData.Date;

            if (!Plugin.Instance.Configuration.DisableGenres)
            {
                foreach (var genreLink in sceneData.Tags)
                {
                    result.Item.AddGenre(genreLink.Name);
                }
            }

            if (!Plugin.Instance.Configuration.DisableActors)
            {
                foreach (var actorLink in sceneData.Performers)
                {
                    string curID = actorLink.UUID,
                        name = actorLink.Name,
                        gender = actorLink.Extras.Gender,
                        role = string.Empty,
                        face = actorLink.Face,
                        image = actorLink.Image;

                    if (actorLink.Parent.HasValue)
                    {
                        curID = actorLink.Parent.Value.UUID;
                        name = actorLink.Parent.Value.Name;
                        if (Plugin.Instance.Configuration.AddDisambiguation && !string.IsNullOrEmpty(actorLink.Parent.Value.Disambiguation))
                        {
                            name += " (" + actorLink.Parent.Value.Disambiguation + ")";
                        }

                        gender = actorLink.Parent.Value.Extras.Gender;
                        face = actorLink.Parent.Value.Face;
                        image = actorLink.Parent.Value.Image;
                    }

                    var actor = new PersonInfo
                    {
                        Name = name,
#if __EMBY__
                        Type = PersonType.Actor,
#else
                        Type = PersonKind.Actor,
#endif
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
                            role = actorLink.Name;
                            break;
                        case ActorsRoleStyle.None:
                            role = string.Empty;
                            break;
                    }

                    if (!string.IsNullOrEmpty(role))
                    {
                        actor.Role = role;
                    }

                    if (Plugin.Instance.Configuration.DisableMaleActors && string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    result.People.Add(actor);
                }

                foreach (var direcorLink in sceneData.Directors)
                {
                    var director = new PersonInfo
                    {
                        Name = direcorLink.Name,
#if __EMBY__
                        Type = PersonType.Director,
#else
                        Type = PersonKind.Director,
#endif
                    };

                    result.People.Add(director);
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
            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Object)
            {
                return result;
            }

            var data = http["data"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Scene>(data);

            var images = new List<(ImageType Type, string Url)>()
            {
                (ImageType.Logo, sceneData.Site.Logo),
            };

            string background = sceneData.Background.Large;
            if (!string.IsNullOrEmpty(background))
            {
                images.Insert(0, (ImageType.Backdrop, (string)background));
            }

            string primary = null;
            switch (Plugin.Instance.Configuration.ScenesImage)
            {
                case ScenesImageStyle.Poster:
                    primary = sceneData.Posters.Large;
                    break;
                case ScenesImageStyle.Background:
                    primary = sceneData.Background.Large;
                    break;
            }

            if (!string.IsNullOrEmpty(primary))
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
            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Array)
            {
                return result;
            }

            var data = http["data"].ToString();
            var searchResults = JsonConvert.DeserializeObject<List<Performer>>(data);

            foreach (var searchResult in searchResults)
            {
                result.Add(new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, searchResult.UUID } },
                    Name = !string.IsNullOrEmpty(searchResult.Disambiguation) ? searchResult.Name + " (" + searchResult.Disambiguation + ")" : (string)searchResult.Name,
                    ImageUrl = searchResult.Image,
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
            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Object)
            {
                return result;
            }

            var data = http["data"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Performer>(data);

            // result.Item.Name = (string)sceneData["name"];
            result.Item.ExternalId = !string.IsNullOrEmpty(sceneData.Disambiguation) ? sceneData.Name + " (" + sceneData.Disambiguation + ")" : (string)sceneData.Name;
            result.Item.OriginalTitle = string.Join(", ", sceneData.Aliases.Select(o => o.ToString().Trim()));
            result.Item.Overview = ActorsOverview.CustomFormat(sceneData);

            var actorBornDate = sceneData.Extras.Birthday;
            if (DateTime.TryParseExact(actorBornDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            var actorBornPlace = sceneData.Extras.Birthplace;
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
            var http = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (http == null || !http.ContainsKey("data") || http["data"].Type != JTokenType.Object)
            {
                return result;
            }

            var data = http["data"].ToString();
            var sceneData = JsonConvert.DeserializeObject<Performer>(data);

            if (Plugin.Instance.Configuration.ActorsImage == ActorsImageStyle.Face)
            {
                if (!string.IsNullOrEmpty(sceneData.Face))
                {
                    var res = GetRemoteImageFromURL(sceneData.Face);

                    result.Add(res);
                }
            }

            foreach (var poster in sceneData.Posters)
            {
                if (!string.IsNullOrEmpty(poster.URL))
                {
                    var res = GetRemoteImageFromURL(poster.URL);

                    result.Add(res);
                }
            }

            return result;
        }

        public static async Task<List<Site>> SiteSearch(string name, CancellationToken cancellationToken)
        {
            List<Site> result = null;

            var url = string.Format(Consts.APISiteSearchURL, Uri.EscapeDataString(name));
            var siteData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (siteData != null && siteData.ContainsKey("data") && siteData["data"].Type == JTokenType.Array)
            {
                var data = (JArray)siteData["data"];
                result = JsonConvert.DeserializeObject<List<Site>>(data.ToString());
            }

            return result;
        }

        public static async Task<Site?> SiteUpdate(string id, CancellationToken cancellationToken)
        {
            Site? result = null;

            var url = string.Format(Consts.APISiteURL, Uri.EscapeDataString(id));
            var siteData = await GetDataFromAPI(url, cancellationToken).ConfigureAwait(false);
            if (siteData != null && siteData.ContainsKey("data") && siteData["data"].Type == JTokenType.Object)
            {
                var data = (JObject)siteData["data"];
                result = JsonConvert.DeserializeObject<Site>(data.ToString());
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
