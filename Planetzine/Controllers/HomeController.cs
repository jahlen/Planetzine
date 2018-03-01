using System;
using System.Collections.Generic;
using System.Linq;
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

        [HttpGet]
        public ActionResult Edit()
        {
            var article = Article.GetSampleArticles()[0]; // Article.New();

            return View(article);
        }
    }
}