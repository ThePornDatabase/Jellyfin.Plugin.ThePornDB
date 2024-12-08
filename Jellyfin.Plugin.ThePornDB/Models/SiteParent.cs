using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct SiteParent
    {
        [JsonProperty(PropertyName = "id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
