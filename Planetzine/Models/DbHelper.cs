using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Documents.Linq;

namespace Planetzine.Models
{
    public class DbHelper
    {
        public static readonly string DatabaseId;
        public static readonly int InitialThroughput;
        public static readonly int MaxConnectionLimit;
        public static ConsistencyLevel ConsistencyLevel;
        public static readonly string EndpointUrl;
        public static readonly string AuthKey;
        public static readonly ConnectionPolicy ConnectionPolicy;

        public static readonly string ServerName;
        public static readonly string ServerSuffix;
        public static readonly DocumentClient Client;
        private static readonly string[] PreferredLocations;
        public static double RequestCharge;

        private static readonly char[] delimiter = new char[] { ',' };

        static DbHelper()
        {
            // Init basic settings
            DatabaseId = ConfigurationManager.AppSettings["DatabaseId"];
            InitialThroughput = int.Parse(ConfigurationManager.AppSettings["InitialThroughput"]);
            MaxConnectionLimit = int.Parse(ConfigurationManager.AppSettings["MaxConnectionLimit"]);
            ConsistencyLevel = (ConsistencyLevel)Enum.Parse(typeof(ConsistencyLevel), ConfigurationManager.AppSettings["ConsistencyLevel"]);

            EndpointUrl = ConfigurationManager.AppSettings["EndpointURL"];
            AuthKey = ConfigurationManager.AppSettings["AuthKey"];

            // Server specific settings
            ServerName = Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME") ?? "local"; // The name of the app service
            ServerSuffix = ServerName.Contains("-") ? $"-{ServerName.Split('-')[1]}" : ""; // Will be -eastus, -westeu or similar

            PreferredLocations = (ConfigurationManager.AppSettings["PreferredLocations" + ServerSuffix] ?? "").Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            // Create connection policy
            ConnectionPolicy = new ConnectionPolicy
            {
                ConnectionMode = (ConnectionMode)Enum.Parse(typeof(ConnectionMode), ConfigurationManager.AppSettings["ConnectionMode"]),
                ConnectionProtocol = (Protocol)Enum.Parse(typeof(Protocol), ConfigurationManager.AppSettings["ConnectionProtocol"]),
                EnableEndpointDiscovery = true,
                MaxConnectionLimit = MaxConnectionLimit,
                RetryOptions = new RetryOptions { MaxRetryAttemptsOnThrottledRequests = 10, MaxRetryWaitTimeInSeconds = 30 }
            };

            foreach (var location in PreferredLocations)
                ConnectionPolicy.PreferredLocations.Add(location);

            Client = new DocumentClient(new Uri(EndpointUrl), AuthKey, ConnectionPolicy, ConsistencyLevel);
            Client.OpenAsync(); // Preload routing tables, to avoid a startup latency on the first request.
        }

        public static async Task CreateDatabase()
        {
            await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });
        }

        public static async Task CreateCollection(string collectionId, string partitionKey)
        {
            var myCollection = new DocumentCollection();
            myCollection.Id = collectionId;
            myCollection.PartitionKey.Paths.Add(partitionKey);

            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                myCollection,
                new RequestOptions { OfferThroughput = InitialThroughput });
        }

        public static async Task DeleteCollection(string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);

            await Client.DeleteDocumentCollectionAsync(uri);
        }

        public static async Task DeleteDatabase()
        {
            var uri = UriFactory.CreateDatabaseUri(DatabaseId);

            await Client.DeleteDatabaseAsync(uri);
        }

    }
}