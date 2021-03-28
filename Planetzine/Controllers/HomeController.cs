using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Planetzine.Models;
using Planetzine.Common;

namespace Planetzine.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index(string tag, string author, string freeTextSearch)
        {
            var articles = new IndexModel();
            if (!string.IsNullOrEmpty(tag))
                articles.Items = await Article.SearchByTag(tag);
            else if (!string.IsNullOrEmpty(author))
                articles.Items = await Article.SearchByAuthor(author);
            else if (!string.IsNullOrEmpty(freeTextSearch))
                articles.Items = await Article.SearchByFreetext(freeTextSearch);
            else
                articles.Items = await Article.GetAll();

            //ViewBag.ConsumedRUs = DbHelper.RequestCharge - initialRequestCharge;
            return View(articles);
        }

        public ActionResult About()
        {
            return View();
        }

        public async Task<ActionResult> ViewArticle(Guid articleId, string author)
        {
            var article = await Article.Read(articleId, author);
            return View(article);
        }

        public async Task<ActionResult> Diagnostics()
        {
            //await Planetzine.MvcApplication.DatabaseReady.Task; // Make sure database and collection is created before continuing

            var diagnostics = new DiagnosticsModel();
            diagnostics.Results = CosmosDbHelper.Diagnostics();
            return View(diagnostics);
        }

        [HttpPost]
        public async Task<ActionResult> Diagnostics(string button)
        {
            button = button.ToLower();
            if (button == "delete")
            {
                await CosmosDbHelper.DeleteDatabaseAsync();
                ViewBag.Message = "Database deleted! It will be recreated next time you restart the application.";
            }
            if (button == "reset")
            {
                //await DbHelper.DeleteCollection(Article.CollectionId);
                //await DbHelper.CreateCollection(Article.CollectionId, Article.PartitionKey);
                await CosmosDbHelper.DeleteAllDocumentsAsync(Article.CollectionId);
                await Article.Create(await Article.GetSampleArticles());
                ViewBag.Message = "Articles deleted and recreated.";
            }

            var diagnostics = new DiagnosticsModel();
            return View(diagnostics);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(Guid? articleId, string author, string message)
        {
            var article = !articleId.HasValue ?
                Article.New() :
                await Article.Read(articleId.Value, author);

            if (!string.IsNullOrEmpty(message))
                ViewBag.Message = message;

            return View(article);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Article article, string TagsStr, string button)
        {
            // Convert comma-separated list of tags to array
            article.Tags = TagsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();
            if (article.Tags.Length == 0)
                ModelState.AddModelError("Tags", "Tags must not be empty.");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("<br/>", ModelState.Values.SelectMany(i => i.Errors).Select(e => e.ErrorMessage));
                ViewBag.Message = $"Error:<br/>{errors}";
                return View(article);
            }

            button = (button ?? "").ToLower();
            switch (button)
            {
                case "save":
                    if (article.IsNew)
                        article.ArticleId = Guid.NewGuid();
                    await article.Upsert();
                    return RedirectToAction("Edit", new { article.ArticleId, article.Author, message = "Article saved" });
                case "preview":
                    ViewBag.EnablePreview = true;
                    break;
                case "delete":
                    await article.Delete();
                    ViewBag.Message = "Article deleted";
                    break;
            }

            return View(article);
        }

        [HttpGet]
        public ActionResult PerformanceTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> PerformanceTest(PerformanceTestModel test)
        {
            try
            {
                await test.RunTests();
                ViewBag.Message = "Tests completed. Run the tests multiple times to get stable results.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"<p>Tests failed.</p><p>{ex.GetType().Name}</p><p>{ex.Message}</p><p>{ex.StackTrace}</p>";
            }
            return View(test);
        }
    }
}