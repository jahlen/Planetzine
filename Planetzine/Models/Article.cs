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
        public Guid ArticleId;

        [JsonProperty("partitionId")]
        public string PartitionId => Author;

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
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime PublishDate;

        [JsonProperty("lastUpdate")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime LastUpdate;

        public static Article New()
        {
            return new Article
            {
                ArticleId = Guid.NewGuid(),
                Heading = "Article Heading",
                ImageUrl = "/Images/earth-11015_640.jpg",
                Body = "Article text here...",
                Tags = new[] { "News" },
                Visible = true,
                Author = "Anonymous",
                PublishDate = DateTime.Now,
                LastUpdate = DateTime.Now
            };
        }

        public string Excerpt => Body.RemoveHtmlTags().GetBeginning(300);

        public string PublishDateStr => PublishDate.ToString("MMMM dd, yyyy").Capitalize();

        public static Article[] GetSampleArticles()
        {
            var article1 = new Article
            {
                ArticleId = Guid.NewGuid(),
                Heading = "Azure Cosmos DB",
                ImageUrl = "/Images/earth-11015_640.jpg",
                Body = "<p><strong>Azure Cosmos DB</strong> is Microsoft’s proprietary globally-distributed, multi-model database service for managing data at planet - scale launched in May 2017. It builds upon and extends the earlier Azure DocumentDB, which was released in 2014. It is schema-less and generally classified as a NoSQL database. </p><h4>Dynamically tunable</h4><p>With the current recommended option of \"partitioned collection\" type, Cosmos DB is dynamically tunable along three dimensions:</p>",
                Tags = new[] { "Microsoft", "Databases" },
                Visible = true,
                Author = "John Doe",
                PublishDate = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            var article2 = new Article
            {
                ArticleId = Guid.NewGuid(),
                Heading = "MongoDB",
                ImageUrl = "/Images/earth-11015_640.jpg",
                Body = "<p><strong>MongoDB</strong> is a free and open-source cross-platform document-oriented database program. Classified as a NoSQL database program, MongoDB uses JSON-like documents with schemas. MongoDB is developed by MongoDB Inc., and is published under a combination of the GNU Affero General Public License and the Apache License.</p><h4>History</h4><p>The software company began developing MongoDB in 2007 as a component of a planned platform as a service product. In 2009, the company shifted to an open source development model, with the company offering commercial support and other services. In 2013, 10gen changed its name to MongoDB Inc.[6]</p><p>On October 20, 2017, MongoDB became a publicly-traded company, listed on NASDAQ as MDB with an IPO price of $24 per share.</p>",
                Tags = new[] { "Open source", "Databases" },
                Visible = true,
                Author = "Jane Doe",
                PublishDate = DateTime.Now,
                LastUpdate = DateTime.Now
            };

            return new[] { article1, article2 };
        }
    }
}