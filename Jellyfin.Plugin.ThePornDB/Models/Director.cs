using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Director
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
