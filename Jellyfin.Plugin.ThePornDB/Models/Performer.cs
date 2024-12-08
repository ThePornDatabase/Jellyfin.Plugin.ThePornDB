using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Performer
    {
        [JsonProperty(PropertyName = "id")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "_id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "disambiguation")]
        public string Disambiguation { get; set; }

        [JsonProperty(PropertyName = "Bio")]
        public string Bio { get; set; }

        [JsonProperty(PropertyName = "aliases")]
        public List<string> Aliases { get; set; }

        [JsonProperty(PropertyName = "face")]
        public string Face { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "extras")]
        public PerformerExtras Extras { get; set; }

        [JsonProperty(PropertyName = "posters")]
        public List<Media> Posters { get; set; }
    }
}
