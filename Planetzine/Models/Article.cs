using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Planetzine.Models
{
    public class Article
    {
        public const string CollectionId = "articles";
        public const string PartitionKey = "/partitionId";

        [JsonProperty("id")]
        public Guid ArticleId { get; set; }

        [JsonProperty("partitionId")]
        public string PartitionId => Author;

        [JsonProperty("heading")]
        [Required]
        public string Heading { get; set; }

        [JsonProperty("imageUrl")]
        public string ImageUrl { get; set; }

        [JsonProperty("body")]
        [Required]
        [AllowHtml]
        public string Body { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        [JsonProperty("author")]
        [Required]
        public string Author { get; set; }

        [JsonProperty("publishDate")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime PublishDate { get; set; }

        [JsonProperty("lastUpdate")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime LastUpdate { get; set; }

        [JsonIgnore]
        public string Excerpt => Body.RemoveHtmlTags().GetBeginning(300);

        [JsonIgnore]
        public string PublishDateStr => PublishDate.ToString("MMMM dd, yyyy").Capitalize();

        [JsonIgnore]
        public string TagsStr => string.Join(",", Tags);

        [JsonIgnore]
        public bool IsNew => ArticleId == Guid.Empty;

        public static Article New()
        {
            return new Article
            {
                ArticleId = Guid.Empty,
                Heading = "Article Heading",
                ImageUrl = "/Images/earth-11015_640.jpg",
                Body = "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed congue ultrices nulla nec malesuada. Etiam vitae risus sit amet dolor ultrices eleifend nec et nisi. Praesent pharetra egestas tortor ut faucibus. Suspendisse blandit nisi eu convallis consequat. Morbi ipsum nisl, viverra id eleifend et, semper gravida risus. Nunc eu erat vel elit feugiat suscipit. Maecenas turpis magna, bibendum vel lectus ac, fermentum tristique ipsum. Proin quis ipsum pretium, lacinia risus a, maximus turpis. Vivamus eu volutpat nibh, in sollicitudin purus.</p>\r\n",
                Tags = new[] { "Azure", "Cloud", "Microsoft" },
                Author = "Anonymous",
                PublishDate = DateTime.Now,
                LastUpdate = DateTime.Now
            };
        }

        public async Task Create()
        {
            await DbHelper.CreateDocument(this, CollectionId);
        }

        public async static Task Create(IEnumerable<Article> articles)
        {
            foreach (var article in articles)
                await article.Create();
        }

        public async Task Upsert()
        {
            await DbHelper.UpsertDocument(this, CollectionId);
        }

        public async Task Delete()
        {
            await DbHelper.DeleteDocument(ArticleId.ToString(), Author, CollectionId);
        }

        public static async Task<Article> Read(Guid articleId, string author)
        {
            var article = await DbHelper.GetDocument<Article>(articleId.ToString(), author, CollectionId);
            return article;
        }

        public static async Task<Article[]> GetAll()
        {
            var articles = await DbHelper.ExecuteQuery<Article>("SELECT * FROM articles", CollectionId, true);
            return articles;
        }

        public static async Task<Article[]> SearchByTag(string tag)
        {
            var articles = await DbHelper.ExecuteQuery<Article>($"SELECT * FROM articles AS a WHERE ARRAY_CONTAINS(a.tags, '{tag}')", CollectionId, true);
            return articles;
        }

        public static async Task<Article[]> SearchByAuthor(string author)
        {
            var articles = await DbHelper.ExecuteQuery<Article>($"SELECT * FROM articles AS a WHERE a.author = '{author}'", CollectionId, true);
            return articles;
        }

        public static async Task<Article[]> SearchByFreetext(string freetext)
        {
            var articles = await DbHelper.ExecuteQuery<Article>($"SELECT * FROM articles AS a WHERE CONTAINS(UPPER(a.body), '{freetext.ToUpper()}')", CollectionId, true);
            return articles;
        }

        public async static Task<long> GetNumberOfArticles()
        {
            var articleCount = await DbHelper.ExecuteScalarQuery<dynamic>("SELECT VALUE COUNT(1) FROM articles", Article.CollectionId, true);
            return articleCount;
        }

        public async static Task<Article[]> GetSampleArticles()
        {
            var titles = new[] { "Cosmos_DB", "Redis", "Voldemort_(distributed_data_store)" };

            var articles = new List<Article>();
            foreach (var title in titles)
            {
                var article = await WikipediaReader.GenerateArticleFromWikipedia(title);
                articles.Add(article);
            }

            return articles.ToArray();
        }
    }
}