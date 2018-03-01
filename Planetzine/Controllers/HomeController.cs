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
        public ActionResult Index()
        {
            var articles = new Articles { Items = Article.GetSampleArticles() };
            return View(articles);
        }

        public ActionResult About()
        {
            return View();
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
        public ActionResult Edit()
        {
            var article = Article.GetSampleArticles()[0]; // Article.New();

            return View(article);
        }

        [HttpPost]
        public ActionResult Edit(Article article, string TagsStr, string button)
        {
            // Convert comma-separated list of tags to array
            article.Tags = TagsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();

            button = (button ?? "").ToLower();
            switch (button)
            {
                case "save":
                    break;
                case "preview":
                    break;
            }

            return View(article);
        }
    }
}