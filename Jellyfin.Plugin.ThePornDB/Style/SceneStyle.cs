using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;
using Newtonsoft.Json.Linq;
using ThePornDB.Configuration;
using ThePornDB.Models;

namespace ThePornDB.Style
{
    public class SceneStyle
    {
        public enum Typ
        {
            Tagline = 1,
            Original = 2,
            Sortable = 3,
        }

        private static readonly string path_list = Path.Combine(Plugin.Instance.DataFolderPath, "data");
        public static string Title(Scene data, Typ field, bool use, string style)
        {
            var format = string.Empty;
            var formatted = string.Empty;
            List<string> no_males = new List<string>();
            List<string> performers = new List<string>();

            foreach (var performer in data.Performers)
            {
                string gender = string.Empty;

                performers.Add(performer.Name);

                if (performer.Parent != null && performer.Parent.HasValue)
                {
                    gender = performer.Parent.Value.Extras.Gender;
                    if (gender != "Male")
                    {
                        no_males.Add(performer.Name);
                    }

                }
            }
            if (use)
            {
                if (File.Exists(Path.Combine(path_list, "scenes.csv")))
                {
                    var dictionary = File.ReadLines(Path.Combine(path_list, "scenes.csv")).Select(line => line.Split(';')).ToDictionary(key => key[0], val => val[((int)field)]);

                    if (dictionary.ContainsKey(data.Site.Name))
                    {
                        format = dictionary.FirstOrDefault(x => x.Key == data.Site.Name).Value;
                    }
                    else if (dictionary.ContainsKey(data.Site.Parent.Value.Name))
                    {
                        format = dictionary.FirstOrDefault(x => x.Key == data.Site.Parent.Value.Name).Value;
                    }
                    else
                    {
                        format = style;
                    }
                }
                else
                {
                    format = style;
                }


                int? site_id = data.Site.ID,
                        parent_id = data.Site.ParentID,
                        network_id = data.Site.NetworkID;

                string parent = string.Empty,
                        network = string.Empty;


                if (parent_id.HasValue && !site_id.Equals(parent_id))
                {            
                    parent = data.Site.Parent.Value.Name;
                }
                if (network_id.HasValue && !site_id.Equals(network_id))
                {
                    network = data.Site.Network.Value.Name;
                }

                DateTime date = (DateTime)data.Date;

                var code = string.Empty;

                if (!string.IsNullOrEmpty(data.ExtID))
                {
                    code = Regex.Replace(data.ExtID, @"\D", string.Empty);
                }

                var parameters = new Dictionary<string, object>()
                {
                    { "{id}", data.ID },
                    { "{uuid}", data.UUID },
                    { "{extid}", data.ExtID },
                    { "{code}", code },
                    { "{title}", data.Title },
                    { "{studio}", data.Site.Name },
                    { "{parent}",parent },
                    { "{network}",network },
                    { "{date_dmy}", date.ToString("dd.MM.yyyy") },
                    { "{date_mdy}", date.ToString("MM/dd/yyyy") },
                    { "{release_date}", date.ToString("yyyy-MM-dd") },
                    { "{actors}", string.Join(", ", performers) },
                    { "{no_male}", string.Join(", ", no_males) },
                };
                formatted = parameters.Aggregate(format, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
                formatted = string.Join(" ", formatted.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return formatted;
        }
        public static List<(ImageType Type, string Url)> ImageList(Scene data)
        {
            var images = new List<(ImageType Type, string Url)>() { };
            switch (Plugin.Instance.Configuration.ScenesImage)
            {
                case ScenesImageStyle.Poster:
                    if (!string.IsNullOrEmpty(data.Posters.Large))
                    {
                        images.Add(new() { Type = ImageType.Primary, Url = data.Posters.Large });
                    }
                    break;
                case ScenesImageStyle.Background:
                    if (!string.IsNullOrEmpty(data.Background.Large))
                    {
                        images.Add(new() { Type = ImageType.Primary, Url = data.Background.Large });
                    }
                    break;
            }

            switch (Plugin.Instance.Configuration.ScenesThumb)
            {
                case ScenesThumbImageStyle.Full:
                    if (!string.IsNullOrEmpty(data.Background.Full))
                    {
                        images.Add(new() { Type = ImageType.Thumb, Url = data.Background.Full });
                    }
                    break;
                case ScenesThumbImageStyle.Large:
                    if (!string.IsNullOrEmpty(data.Background.Large))
                    {
                        images.Add(new() { Type = ImageType.Thumb, Url = data.Background.Large });
                    }
                    break;
            }


            switch (Plugin.Instance.Configuration.ScenesBackdrop)
            {
                case ScenesBackdropImageStyle.Full:
                    if (!string.IsNullOrEmpty(data.Background.Full))
                    {
                        images.Add(new() { Type = ImageType.Backdrop, Url = data.Background.Full });
                    }
                    break;
                case ScenesBackdropImageStyle.Large:
                    if (!string.IsNullOrEmpty(data.Background.Large))
                    {
                        images.Add(new() { Type = ImageType.Backdrop, Url = data.Background.Large });
                    }
                    break;
            }
            if (!string.IsNullOrEmpty(data.Site.Logo))
            {
                images.Add(new() { Type = ImageType.Logo, Url = data.Site.Logo });
            }

            return images;

        }
    }
}
