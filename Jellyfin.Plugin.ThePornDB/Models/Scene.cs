using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ThePornDB.Models
{
    public struct Scene
    {
        [JsonProperty(PropertyName = "id")]
        public string UUID { get; set; }

        [JsonProperty(PropertyName = "_id")]
        public int ID { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "trailer")]
        public string Trailer { get; set; }

        [JsonProperty(PropertyName = "date")]
        public DateTime? Date { get; set; }

        [JsonProperty(PropertyName = "site")]
        public Site Site { get; set; }

        [JsonProperty(PropertyName = "performers")]
        public List<PerformerSite> Performers { get; set; }

        [JsonProperty(PropertyName = "directors")]
        public List<Director> Directors { get; set; }

        [JsonProperty(PropertyName = "tags")]
        public List<Tags> Tags { get; set; }

        [JsonProperty(PropertyName = "poster")]
        public string Poster { get; set; }

        [JsonProperty(PropertyName = "background")]
        public Image Background { get; set; }

        [JsonProperty(PropertyName = "posters")]
        public Image Posters { get; set; }
    }
}
