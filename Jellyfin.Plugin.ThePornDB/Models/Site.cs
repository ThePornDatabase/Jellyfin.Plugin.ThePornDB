using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Site
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "uuid")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "poster")]
        public string Poster { get; set; }

        [JsonProperty(PropertyName = "logo")]
        public string Logo { get; set; }

        [JsonProperty(PropertyName = "favicon")]
        public string Favicon { get; set; }

        [JsonProperty(PropertyName = "parent_id")]
        public int? ParentID { get; set; }

        [JsonProperty(PropertyName = "parent")]
        public SiteParent? Parent { get; set; }

        [JsonProperty(PropertyName = "network_id")]
        public int? NetworkID { get; set; }

        [JsonProperty(PropertyName = "network")]
        public SiteParent? Network { get; set; }
    }
}
