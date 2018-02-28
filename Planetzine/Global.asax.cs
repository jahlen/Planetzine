using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Planetzine.Models;

namespace Planetzine
{
    public class MvcApplication : System.Web.HttpApplication
    {
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
            //await DbHelper.CreateDatabase();
            //await DbHelper.CreateCollection(Article.CollectionId, Article.PartitionKey);
        }
    }
}
