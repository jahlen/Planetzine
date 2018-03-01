using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Planetzine.Models;

namespace Planetzine.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index(string tag, string author)
        {
            await Planetzine.MvcApplication.DatabaseReady.Task;

            var articles = new Articles();
            if (!string.IsNullOrEmpty(tag))
                articles.Items = await Article.SearchByTag(tag);
            else if (!string.IsNullOrEmpty(author))
                articles.Items = await Article.SearchByAuthor(author);
            else
                articles.Items = await Article.GetAll();

            return View(articles);
        }

        public ActionResult About()
        {
            return View();
        }

        public async Task<ActionResult> View(Guid articleId, string author)
        {
            var article = await Article.Read(articleId, author);
            return View(article);
        }

        public ActionResult Diagnostics()
        {
            var diagnostics = new Diagnostics();
            diagnostics.Results = DbHelper.Diagnostics();
            return View(diagnostics);
        }

        [HttpPost]
        public async Task<ActionResult> Diagnostics(string button)
        {
            if (button == "delete")
            {
                await DbHelper.DeleteDatabase();
                ViewBag.Message = "Database deleted!";
            }

            return Diagnostics();
        }

        [HttpGet]
        public async Task<ActionResult> Edit(Guid articleId, string author, string message)
        {
            var article = (articleId == Guid.Empty) ? Article.New() : await Article.Read(articleId, author);

            if (!string.IsNullOrEmpty(message))
                ViewBag.Message = message;

            return View(article);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(Article article, string TagsStr, string button)
        {
            // Convert comma-separated list of tags to array
            article.Tags = TagsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();

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
    }
}