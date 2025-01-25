using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct PerformerSite
    {
        [JsonProperty(PropertyName = "id")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "_id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "face")]
        public string Face { get; set; }

        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        [JsonProperty(PropertyName = "parent")]
        public Performer? Parent { get; set; }

        [JsonProperty(PropertyName = "extras")]
        public PerformerExtras Extras { get; set; }
    }
}
