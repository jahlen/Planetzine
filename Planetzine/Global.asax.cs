using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Planetzine.Models;

namespace Planetzine
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static TaskCompletionSource<bool> DatabaseReady = new TaskCompletionSource<bool>();

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitDatabase();
        }

        private async void InitDatabase()
        {
            // Init must be called before using the DbHelper
            await DbHelper.Init();

            // Create the database and collection if it doesn't already exist
            await DbHelper.CreateDatabase();
            await DbHelper.CreateCollection(Article.CollectionId, Article.PartitionKey);

            // If the database if empty, insert some sample articles
            if (await Article.GetNumberOfArticles() == 0)
                await Article.Create(await Article.GetSampleArticles());

            DatabaseReady.SetResult(true);
        }
    }
}
