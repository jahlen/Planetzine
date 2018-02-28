using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Planetzine.Models
{
    public class Article
    {
        public const string CollectionId = "players";
        public const string PartitionKey = "/partitionId";

        [JsonProperty("id")]
        public Guid Id;

        [JsonProperty("partitionId")]
        public string PartitionId => Author;

        [JsonProperty("slug")]
        public string Slug;

        [JsonProperty("heading")]
        public string Heading;

        [JsonProperty("imageUrl")]
        public string ImageUrl;

        [JsonProperty("body")]
        public string Body;

        [JsonProperty("tags")]
        public string[] Tags;

        [JsonProperty("visible")]
        public bool Visible;

        [JsonProperty("author")]
        public string Author;

        [JsonProperty("publishDate")]
        public DateTime PublishDate;

        [JsonProperty("lastUpdate")]
        public DateTime LastUpdate;
    }
}