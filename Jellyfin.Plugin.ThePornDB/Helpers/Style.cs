using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ICU4N.Util;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using ThePornDB.Models;

namespace ThePornDB.Helpers
{
    public class Style
    {
        public enum Typ
        {
            Tagline = 1,
            Original = 2,
            Sortable = 3,
        }
        private static readonly string path_list = Path.Combine(Plugin.Instance.DataFolderPath, "data");
        public static string Title(Scene data, Typ field, bool useStyle, string confStyle)
        {
            var format = string.Empty;
            var formatted = string.Empty;
            List<string> actors = new List<string>();
            List<string> no_males = new List<string>();

            foreach (var actor in data.Performers)
            {
                string gender = string.Empty;

                actors.Add(actor.Name);

                if (actor.Parent != null && actor.Parent.HasValue)
                {
                    if (actor.Parent.Value.Extras.Gender != null)
                    {
                        gender = actor.Parent.Value.Extras.Gender;

                        if (gender != "Male")
                        {
                            no_males.Add(actor.Name);
                        }
                    }
                }
            }

    

            if (useStyle)
            {
                if (File.Exists(Path.Combine(path_list, "sites.csv")))
                {
                    var dictionary = File.ReadLines(Path.Combine(path_list, "sites.csv")).Select(line => line.Split(';')).ToDictionary(key => key[0], val => val[((int)field)]);

                    if (dictionary.ContainsKey(data.Site.Name))
                    {
                        format = dictionary.FirstOrDefault(x => x.Key == data.Site.Name).Value;
                    }
                    else
                    {
                        format = confStyle;
                    }
                }
                else
                {
                    format = confStyle;
                }

                int? site_id = data.Site.ID,
                    parent_id = data.Site.ParentID;
                string network;

                if (parent_id.HasValue && !site_id.Equals(parent_id))
                {
                    if (data.Site.Parent.HasValue)
                    {
                        network = data.Site.Parent.Value.Name;    
                    }
                    else
                    {
                        network = data.Site.Name;
                    }

                    var parameters = new Dictionary<string, object>()
                {
                    { "{id}", data.ID },
                    { "{uuid}", data.UUID },
                    { "{extid}", data.ExtID },
                    { "{title}", data.Title },
                    { "{studio}", data.Site.Name },
                    { "{network}",network },
                    { "{release_date}", data.Date },
                    { "{actors}", string.Join(", ", actors) },
                    { "{no_male}", string.Join(", ", no_males) },
                };
                formatted = parameters.Aggregate(format, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
                formatted = string.Join(" ", formatted.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return formatted;
        }
    
   
 
        public static (string Overview, string[] Tags) Actor(Performer actorData,string confStyle)
        {

            var cup = actorData.Extras.CupSize;

            if (!string.IsNullOrEmpty(cup))
            {
                cup = Regex.Replace(cup, "[0-9]".ToUpper(), string.Empty);
            }
            else { cup = string.Empty; }

            var placeholders = new Dictionary<string, string>()
            {
                { "{cup}", cup },
                { "{bio}", actorData.Bio },
                { "{hips}", actorData.Extras.Hips },
                { "{waist}", actorData.Extras.Waist },
                { "{active}", actorData.Extras.Active },
                { "{height}", actorData.Extras.Height },
                { "{weight}", actorData.Extras.Weight },
                { "{gender}", actorData.Extras.Gender },
                { "{cupsize}", actorData.Extras.CupSize },
                { "{tattoos}", actorData.Extras.Tattoos },
                { "{birthday}", actorData.Extras.Birthday },
                { "{eye_color}", actorData.Extras.EyeColour},
                { "{piercings}", actorData.Extras.Piercings },
                { "{ethnicity}", actorData.Extras.Ethnicity },
                { "{astrology}", actorData.Extras.Astrology },
                { "{birthplace}", actorData.Extras.Birthplace },
                { "{hair_color}", actorData.Extras.HairColour },
                { "{nationality}", actorData.Extras.Nationality },
                { "{measurements}", actorData.Extras.Measurements },
            };
            string overview = confStyle;
            overview = placeholders.Aggregate(overview, (current, parameter) => current.Replace(parameter.Key, parameter.Value));

            string taglist = confStyle;
            taglist = taglist.Replace("{bio}", "");
            taglist = placeholders.Aggregate(taglist, (current, parameter) => current.Replace(parameter.Key, parameter.Value));
            List<string> tags = new List<string>(taglist.Split(','));

            return (overview, tags.ToArray());
        }
    }
}
