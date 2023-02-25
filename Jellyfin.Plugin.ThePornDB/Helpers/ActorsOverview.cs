using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThePornDB.Configuration;

namespace ThePornDB.Helpers
{
    public class ActorsOverview
    {
        public static string CustomFormat(JObject actorData)
        {
            var placeholders = new Dictionary<string, string>()
            {
                { "{bio}", (string)actorData["bio"] },
                { "{hips}", (string)actorData["extras"]["hips"] },
                { "{waist}", (string)actorData["extras"]["waist"] },
                { "{active}", (string)actorData["extras"]["active"] },
                { "{height}", (string)actorData["extras"]["height"] },
                { "{weight}", (string)actorData["extras"]["weight"] },
                { "{gender}", (string)actorData["extras"]["gender"] },
                { "{cupsize}", (string)actorData["extras"]["cupsize"] },
                { "{tattoos}", (string)actorData["extras"]["tattoos"] },
                { "{birthday}", (string)actorData["extras"]["birthday"] },
                { "{eye_color}", (string)actorData["extras"]["eye_colour"] },
                { "{piercings}", (string)actorData["extras"]["piercings"] },
                { "{ethnicity}", (string)actorData["extras"]["ethnicity"] },
                { "{astrology}", (string)actorData["extras"]["astrology"] },
                { "{birthplace}", (string)actorData["extras"]["birthplace"] },
                { "{hair_color}", (string)actorData["extras"]["hair_colour"] },
                { "{nationality}", (string)actorData["extras"]["nationality"] },
                { "{measurements}", (string)actorData["extras"]["measurements"] },
            };

            string overview = Plugin.Instance.Configuration.ActorsOverviewFormat;
            
            switch ((string)actorData["extras"]["gender"])
            {
                case "Female":                    
                    overview = Regex.Replace(overview, @"<male>.*?</male>|<trans>.*?</trans>", string.Empty);
                    break;
                case "Trans":
                    overview = Regex.Replace(overview, @"<female>.*?</female>|<male>.*?</male>", string.Empty);
                    break;
                case "Male":
                    overview = Regex.Replace(overview, @"<female>.*?</female>|<trans>.*?</trans>", string.Empty);
                    break;
                default:
                    overview = Regex.Replace(overview, @"<female>.*?</female>|<trans>.*?</trans>|<male>.*?</male>", string.Empty);
                    break;
            }
            
            overview = Regex.Replace(overview, @"<.?male.|<.?female.|<.?trans.", string.Empty);
            overview = placeholders.Aggregate(overview, (current, parameter) => current.Replace(parameter.Key, parameter.Value));
            //overview = Regex.Replace(overview, @"{.*?}", string.emty);

            return overview;
        }
    }
}
