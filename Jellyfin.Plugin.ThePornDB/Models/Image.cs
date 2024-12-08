using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Image
    {
        [JsonProperty(PropertyName = "full")]
        public string Full { get; set; }

        [JsonProperty(PropertyName = "large")]
        public string Large { get; set; }
    }
}
