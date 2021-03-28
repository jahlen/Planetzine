using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Planetzine.Common
{
    public class CosmosDbSetup : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
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

            try
            {
                // Init must be called before using the DbHelper
                await CosmosDbHelper.InitAsync();

                // Create the database and collection if it doesn't already exist
                Trace.WriteLine($"Creating database {CosmosDbHelper.DatabaseId} (if not already exists)");
                await CosmosDbHelper.CreateDatabaseAsync();
                await CosmosDbHelper.CreateCollectionAsync(Article.CollectionId, Article.PartitionKey);

                // If the database if empty, insert some sample articles
                if (await Article.GetNumberOfArticles() == 0)
                {
                    Trace.WriteLine("Creating sample articles");
                    await Article.Create(await Article.GetSampleArticles());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to init database.", ex);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
