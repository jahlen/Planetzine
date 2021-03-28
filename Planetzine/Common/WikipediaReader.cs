using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Planetzine.Common
{
    public static class WikipediaReader
    {
        public const string Endpoint = "https://en.wikipedia.org/w/api.php";
        private static readonly Regex regexRef = new Regex(@"<ref>.*?<\/ref>");
        private static readonly Regex regexMeta = new Regex(@"{{[^{}]+}}");
        private static readonly Regex regexStrong = new Regex(@"'''(.*?)'''");
        private static readonly Regex regexHeading = new Regex(@"==(.*?)==");
        private static readonly Regex regexCategory = new Regex(@"\[\[Category:(.*?)\]\]");
        private static readonly Regex regexLink = new Regex(@"\[\[([^|\]]+)(|[^\[\]]+)?\]\]");

        public static string CreateQueryUrl(string title)
        {
            return $"{Endpoint}?action=query&titles={HttpUtility.UrlEncode(title)}&prop=revisions&rvprop=content|user|tags|timestamp&format=json&formatversion=2";
        }

        public static async Task<string> DownloadWikipediaArticle(string title)
        {
            var client = new WebClient();
            client.Headers.Add("user-agent", "Planetzine/1.0 (https://github.com/jahlen/Planetzine)");
            var data = await client.DownloadDataTaskAsync(CreateQueryUrl(title));
            var str = Encoding.UTF8.GetString(data);
            return str;
        }

        public static async Task<Article> GenerateArticleFromWikipedia(string title)
        {
            var str = await DownloadWikipediaArticle(title);
            var jobj = JObject.Parse(str);
            var page = jobj["query"]["pages"].First();
            var revision = page["revisions"].First();
            var content = revision.Value<string>("content");

            var article = new Article
            {
                ArticleId = Guid.NewGuid(),
                Heading = page.Value<string>("title"),
                Body = WikiToHtml(content),
                Tags = regexCategory.Matches(content).OfType<Match>().Select(m => m.Groups[1].Value).ToArray(),
                Author = revision.Value<string>("user"),
                PublishDate = revision.Value<DateTime>("timestamp"),
                LastUpdate = DateTime.Now,
                ImageUrl = "/Images/earth-11015_640.jpg"
            };

            return article;
        }

        public static string WikiToHtml(string wikiStr)
        {
            var str = wikiStr;
            str = regexCategory.Replace(str, ""); // Remove categories
            str = regexRef.Replace(str, ""); // Remove all references

            int strlen;
            do  // Remove {{ }} - and iterate until nothing changes
            {
                strlen = str.Length;
                str = regexMeta.Replace(str, "");
            } while (str.Length != strlen);

            str = regexStrong.Replace(str, match => $"<strong>{match.Groups[1].Value}</strong>"); // Convert strong html
            str = regexHeading.Replace(str, match => $"<h4>{match.Groups[1].Value}</h4>"); // Convert headings to proper html
            str = regexLink.Replace(str, match => $"{match.Groups[1].Value}"); // Convert links
            str = str.Trim();
            str = str.Replace("\n", "<br/>");

            return str;
        }
    }
}