using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;

namespace ThePornDB.Helpers
{
     public class ActorsOverview
    {
        public static string CustomFormat(JObject actorData)
        {

            var placeholders = new List<(string placeholder, string value)>()
            {
                ("Bio", (string)actorData["bio"]),
                ("Hips", (string)actorData["extras"]["hips"]),
                ("Waist", (string)actorData["extras"]["waist"]),
                ("Active", (string)actorData["extras"]["active"]),
                ("Height", (string)actorData["extras"]["height"]),
                ("Weight", (string)actorData["extras"]["weight"]),
                ("Gender", (string)actorData["extras"]["gender"]),
                ("Cupsize", (string)actorData["extras"]["cupsize"]),
                ("Tattoos", (string)actorData["extras"]["tattoos"]),
                ("Birthday", (string)actorData["extras"]["birthday"]),
                ("Eye Color",(string)actorData["extras"]["eye_colour"]),
                ("Piercings", (string)actorData["extras"]["piercings"]),
                ("Ethnicity", (string)actorData["extras"]["ethnicity"]),
                ("Astrology", (string)actorData["extras"]["astrology"]),
                ("Birthplace", (string)actorData["extras"]["birthplace"]),
                ("Hair Color", (string)actorData["extras"]["hair_colour"]),
                ("Nationality", (string)actorData["extras"]["nationality"]),
                ("Measurements", (string)actorData["extras"]["measurements"]),
            };
          
            string overview = Plugin.Instance.Configuration.ActorsOverviewFormat;
            

            switch (Plugin.Instance.Configuration.ActorsOverview)
            {
               
                case 2:
                    {
                        foreach (var ph in placeholders)
                        {
                            if (!string.IsNullOrEmpty(ph.value))
                            {
                                string repl = string.Concat(ph.placeholder[0].ToString().ToUpper(), ph.placeholder.AsSpan(1), " : ");
                                overview = overview.Replace($"{{{ph.placeholder}}}", repl + ph.value);
                            }
                            else
                            {
                                overview = overview.Replace($"{{{ph.placeholder}}}", string.Empty);
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        overview = (string)actorData["bio"];
                        break;
                    }
                default:
                    {
                        overview = " ";
                        break;
                    }
            }
          
            return overview;
        }
    }
}
