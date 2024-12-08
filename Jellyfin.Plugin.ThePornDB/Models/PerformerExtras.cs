using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct PerformerExtras
    {
        [JsonProperty(PropertyName = "hips")]
        public string Hips { get; set; }

        [JsonProperty(PropertyName = "waist")]
        public string Waist { get; set; }

        [JsonProperty(PropertyName = "active")]
        public string Active { get; set; }

        [JsonProperty(PropertyName = "height")]
        public string Height { get; set; }

        [JsonProperty(PropertyName = "weight")]
        public string Weight { get; set; }

        [JsonProperty(PropertyName = "gender")]
        public string Gender { get; set; }

        [JsonProperty(PropertyName = "cupsize")]
        public string CupSize { get; set; }

        [JsonProperty(PropertyName = "tattoos")]
        public string Tattoos { get; set; }

        [JsonProperty(PropertyName = "birthday")]
        public string Birthday { get; set; }

        [JsonProperty(PropertyName = "eye_colour")]
        public string EyeColour { get; set; }

        [JsonProperty(PropertyName = "piercings")]
        public string Piercings { get; set; }

        [JsonProperty(PropertyName = "ethnicity")]
        public string Ethnicity { get; set; }

        [JsonProperty(PropertyName = "astrology")]
        public string Astrology { get; set; }

        [JsonProperty(PropertyName = "birthplace")]
        public string Birthplace { get; set; }

        [JsonProperty(PropertyName = "hair_colour")]
        public string HairColour { get; set; }

        [JsonProperty(PropertyName = "nationality")]
        public string Nationality { get; set; }

        [JsonProperty(PropertyName = "measurements")]
        public string Measurements { get; set; }
    }
}
