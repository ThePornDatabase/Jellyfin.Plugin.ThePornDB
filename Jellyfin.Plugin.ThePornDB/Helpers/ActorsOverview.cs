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
             
            };

            string overview = Plugin.Instance.Configuration.ActorsOverviewFormat;
            foreach (var extraLink in actorData["extras"].Children())
            {
                JProperty extraProp = (JProperty)extraLink;
                placeholders.Add("{" + extraProp.Name.ToLower() + "}", (string)extraLink);

                if ((string)extraLink != null)
                {
                    placeholders.Add("{" + extraProp.Name.ToUpper() + "}", char.ToUpper(extraProp.Name[0]) + extraProp.Name.Substring(1) + " :");
                }
            }
            
            switch (Plugin.Instance.Configuration.ActorsOverview)
            {
                case ActorsOverviewStyle.CustomExtras:
                    overview = placeholders.Aggregate(overview, (current, parameter) => current.Replace(parameter.Key, parameter.Value));
                    overview = Regex.Replace(overview, @"{.*?}", string.Empty);
                    break;
                case ActorsOverviewStyle.Default:
                    overview = (string)actorData["bio"];
                    break;
              case ActorsOverviewStyle.None:
                    overview = " ";
                    break;
                default:
                    overview = string.Empty;
                    break;
            }

            return overview;
        }
    }
}
