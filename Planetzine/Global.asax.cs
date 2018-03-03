using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Planetzine.Models;
using Planetzine.Common;

namespace Planetzine
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static TaskCompletionSource<Exception> DatabaseReady = new TaskCompletionSource<Exception>();

        public double InitialRequestCharge;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Task.Run(InitDatabase);

            // Check the EndpointURL
            try
            {
                var endpoint = ConfigurationManager.AppSettings["EndpointURL"];
                var uri = new Uri(endpoint);
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid EndpointURL. Check Web.config or your App Settings.");
            }

            // Make sure database and collection is created before continuing
            var dbCompletedInitialization = DatabaseReady.Task.Wait(TimeSpan.FromSeconds(60));
            if (!dbCompletedInitialization)
                throw new Exception("Database initialization timed out");

            // Check if an exception occured during initialization
            if (DatabaseReady.Task.Result != null)
                throw DatabaseReady.Task.Result;
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            InitialRequestCharge = DbHelper.RequestCharge;
        }

        public static double GetConsumedRUs()
        {
            return DbHelper.RequestCharge - ((MvcApplication)HttpContext.Current.ApplicationInstance).InitialRequestCharge;
        }

        private async Task InitDatabase()
        {
            try
            {
                // Init must be called before using the DbHelper
                await DbHelper.InitAsync();

                // Create the database and collection if it doesn't already exist
                await DbHelper.CreateDatabaseAsync();
                await DbHelper.CreateCollectionAsync(Article.CollectionId, Article.PartitionKey);

                // If the database if empty, insert some sample articles
                if (await Article.GetNumberOfArticles() == 0)
                    await Article.Create(await Article.GetSampleArticles());

                DatabaseReady.SetResult(null);
            }
            catch (Exception ex)
            {
                DatabaseReady.SetResult(ex);
            }
        }
    }
}
