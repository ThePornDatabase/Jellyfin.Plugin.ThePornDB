using System.Collections.Generic;
using System.Linq;
using ThePornDB.Configuration;
using ThePornDB.Models;

namespace ThePornDB.Helpers
{
    public class ActorsOverview
    {
        public static string CustomFormat(Performer actorData)
        {
            var placeholders = new Dictionary<string, string>()
            {
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
                { "{eye_color}", actorData.Extras.EyeColour },
                { "{piercings}", actorData.Extras.Piercings },
                { "{ethnicity}", actorData.Extras.Ethnicity },
                { "{astrology}", actorData.Extras.Astrology },
                { "{birthplace}", actorData.Extras.Birthplace },
                { "{hair_color}", actorData.Extras.HairColour },
                { "{nationality}", actorData.Extras.Nationality },
                { "{measurements}", actorData.Extras.Measurements },
            };

            string overview = Plugin.Instance.Configuration.ActorsOverviewFormat;
            switch (Plugin.Instance.Configuration.ActorsOverview)
            {
                case ActorsOverviewStyle.CustomExtras:
                    overview = placeholders.Aggregate(Plugin.Instance.Configuration.ActorsOverviewFormat, (current, parameter) => current.Replace(parameter.Key, parameter.Value));
                    break;
                case ActorsOverviewStyle.Default:
                    overview = (string)actorData.Bio;
                    break;
                default:
                    overview = " ";
                    break;
            }

            return overview;
        }
    }
}
