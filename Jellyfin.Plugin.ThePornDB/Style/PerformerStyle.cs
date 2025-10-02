using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using ThePornDB.Configuration;
using ThePornDB.Models;

namespace ThePornDB.Style
{
    public class PerformerStyle
    {
        public static (string Descripton, string[] Tags) Style (Performer data)
        {

            var cup = string.Empty;

            var detail = data.Extras;

            if (!string.IsNullOrEmpty(detail.CupSize))
            {
                cup = Regex.Replace(detail.CupSize, "[0-9]".ToUpper(), string.Empty);
            }

            string boobs = String.Format("Boobs: {1} Fake: {0}", detail.FakeBoobs ? "Yes" : "No", cup);

            string career = String.Format("Start: {0} End: {1}", detail.CareerStart, detail.CareerEnd);

            string fakeboobs = String.Format("{0}",detail.FakeBoobs ? "Yes": "No");
            string samesexonly = String.Format("{0}", detail.SameSexOnly ? "Yes" : "No");


            var placeholders = new Dictionary<string, string>()
            {
                { "{cup}", cup },
                { "{po*}", "Porn ★ " },
                { "{boobs_strg}", boobs },
                { "{career_strg}", career },
                { "{bio}", data.Bio },
                { "{hips}", detail.Hips },
                { "{waist}", detail.Waist },
                { "{active}", detail.Active },
                { "{height}", detail.Height},
                { "{weight}", detail.Weight },
                { "{gender}", detail.Gender },
                { "{cupsize}", detail.CupSize },
                { "{tattoos}", detail.Tattoos },
                { "{birthday}", detail.Birthday },
                { "{deathday}", detail.Deathday},
                { "{piercings}", detail.Piercings },
                { "{ethnicity}", detail.Ethnicity },
                { "{astrology}", detail.Astrology },
                { "{birthplace}", detail.Birthplace },
                { "{eye_colour}" ,detail.EyeColour },
                { "{hair_color}", detail.HairColour },
                { "{nationality}", detail.Nationality },
                { "{measurements}", detail.Measurements },
                { "{fakeboobs}", fakeboobs },
                { "{career_end}", detail.CareerEnd },
                { "{career_start}", detail.CareerStart },
                { "{same_sex_only}", samesexonly },
                
              
            };

            switch (detail.Gender)
            {
                case "Male":
                    placeholders.Add("{tag}", "♂");
                    break;
                case "Female":
                    placeholders.Add("{tag}", "♀");
                    break;
                case "Intersex":
                    placeholders.Add("{tag}", "ⴲ");
                    break;
                case "Non Binary":
                    placeholders.Add("{tag}", "⚲");
                    break;
                case "Transgender Male":
                    placeholders.Add("{tag}", "⚥");
                    break;
                case "Transgender Female":
                    placeholders.Add("{tag}", "⚥");
                    break;
                default:
                    placeholders.Add("{tag}", "?");
                    break;
            }

            string tags = string.Empty;

            switch (Plugin.Instance.Configuration.ActorsTagStyle)
            {
                case (ActorsTagStyle.Custom):
                    tags = Plugin.Instance.Configuration.CustomTagActors;
                    tags = tags.Replace("{bio}", string.Empty);
                    break;
                case (ActorsTagStyle.Ethnicity):
                    tags = detail.Ethnicity;
                    break;
                case (ActorsTagStyle.Nationality):
                    tags = detail.Nationality;
                    break;

            }
            var description = string.Empty;

            switch (Plugin.Instance.Configuration.ActorsOverviewStyle)
            {
                case ActorsOverviewStyle.Custom:
                    description = Plugin.Instance.Configuration.ActorsOverviewFormat;
                    break;
                case ActorsOverviewStyle.Default:
                    description = data.Bio;
                    break;
                case ActorsOverviewStyle.None:
                    description = string.Empty;
                    break;
            }


            tags = placeholders.Aggregate(tags, (current, parameter) => current.Replace(parameter.Key, parameter.Value));

            description = placeholders.Aggregate(description, (current, parameter) => current.Replace(parameter.Key, parameter.Value));

            string[] tag = tags.Split(',');

            return (description, tag);
        }
        
    }
}
