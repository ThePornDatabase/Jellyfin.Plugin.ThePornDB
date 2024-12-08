using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Media
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string URL { get; set; }

        [JsonProperty(PropertyName = "size")]
        public int Size { get; set; }

        [JsonProperty(PropertyName = "order")]
        public int Order { get; set; }
    }
}
