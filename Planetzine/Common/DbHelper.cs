using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Documents.Linq;

namespace Planetzine.Common
{
    public class DbHelper
    {
        public static readonly string DatabaseId;
        public static readonly int InitialThroughput;
        public static readonly int MaxConnectionLimit;
        public static readonly ConsistencyLevel ConsistencyLevel;
        public static readonly string EndpointUrl;
        public static readonly string AuthKey;

        public static string CurrentRegion;
        public static ConnectionPolicy ConnectionPolicy;
        public static DocumentClient Client;
        public static double RequestCharge;

        static DbHelper()
        {
            // Init basic settings
            DatabaseId = ConfigurationManager.AppSettings["DatabaseId"];
            InitialThroughput = int.Parse(ConfigurationManager.AppSettings["InitialThroughput"]);
            MaxConnectionLimit = int.Parse(ConfigurationManager.AppSettings["MaxConnectionLimit"]);
            ConsistencyLevel = (ConsistencyLevel)Enum.Parse(typeof(ConsistencyLevel), ConfigurationManager.AppSettings["ConsistencyLevel"]);

            EndpointUrl = ConfigurationManager.AppSettings["EndpointURL"];
            AuthKey = ConfigurationManager.AppSettings["AuthKey"];
        }

        /// <summary>
        /// Init() method must be called before using any other methods on DbHelper. Creates the DocumentClient.
        /// </summary>
        /// <returns></returns>
        public static async Task InitAsync()
        {
            CurrentRegion = GetCurrentAzureRegion();

            // Create connection policy
            ConnectionPolicy = new ConnectionPolicy
            {
                ConnectionMode = (ConnectionMode)Enum.Parse(typeof(ConnectionMode), ConfigurationManager.AppSettings["ConnectionMode"]),
                ConnectionProtocol = (Protocol)Enum.Parse(typeof(Protocol), ConfigurationManager.AppSettings["ConnectionProtocol"]),
                EnableEndpointDiscovery = true,
                MaxConnectionLimit = MaxConnectionLimit,
                RetryOptions = new RetryOptions { MaxRetryAttemptsOnThrottledRequests = 10, MaxRetryWaitTimeInSeconds = 30 }
            };
            ConnectionPolicy.PreferredLocations.Add(await GetNearestAzureReadRegionAsync());

            Client = new DocumentClient(new Uri(EndpointUrl), AuthKey, ConnectionPolicy, ConsistencyLevel);
            await Client.OpenAsync(); // Preload routing tables, to avoid a startup latency on the first request.
        }

        private static async Task<IEnumerable<DatabaseAccountLocation>> GetAvailableAzureReadRegionsAsync()
        {
            using (var client = new DocumentClient(new Uri(EndpointUrl), AuthKey, ConnectionPolicy.Default))
            {
                var account = await client.GetDatabaseAccountAsync();
                return account.ReadableLocations;
            }
        }

        private static string GetCurrentAzureRegion()
        {
            return Environment.GetEnvironmentVariable("REGION_NAME") ?? "local";
        }

        private static async Task<string> GetNearestAzureReadRegionAsync()
        {
            var regions = (await GetAvailableAzureReadRegionsAsync()).ToDictionary(region => region.Name);
            var currentRegion = GetCurrentAzureRegion();

            // If there is a readable location in the current region, chose it
            if (regions.ContainsKey(currentRegion))
                return currentRegion;

            // Otherwise just pick the first region
            // TODO: Replace this with some logic that selects a more optimal read region (for instance using a table)
            return regions.Values.First().Name;
        }

        public static async Task CreateDatabaseAsync()
        {
            await Client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });
        }

        public static async Task CreateCollectionAsync(string collectionId, string partitionKey)
        {
            var myCollection = new DocumentCollection();
            myCollection.Id = collectionId;
            myCollection.PartitionKey.Paths.Add(partitionKey);

            await Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                myCollection,
                new RequestOptions { OfferThroughput = InitialThroughput });
        }

        public static async Task DeleteCollectionAsync(string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);

            await Client.DeleteDocumentCollectionAsync(uri);
        }

        public static async Task DeleteDatabaseAsync()
        {
            var uri = UriFactory.CreateDatabaseUri(DatabaseId);

            await Client.DeleteDatabaseAsync(uri);
        }

        public static async Task CreateDocumentAsync(object document, string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var response = await Client.CreateDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task UpsertDocumentAsync(object document, string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var response = await Client.UpsertDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task ReplaceDocumentAsync(object document, string documentId, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);
            var response = await Client.ReplaceDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task<DocumentResponse<T>> GetDocumentAsync<T>(string documentId, object partitionKey, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);

            var response = await Client.ReadDocumentAsync<T>(uri, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            RequestCharge += response.RequestCharge;
            
            return response;
        }

        public static async Task DeleteDocumentAsync(string documentId, object partitionKey, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);

            var response = await Client.DeleteDocumentAsync(uri, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            RequestCharge += response.RequestCharge;
        }

        public static async Task DeleteAllDocumentsAsync(string collectionId)
        {
            var documents = await ExecuteQueryAsync<dynamic>($"SELECT c.id, c.partitionId FROM {collectionId} AS c", collectionId, true);
            foreach (var document in documents)
            {
                await DeleteDocumentAsync(document.id, document.partitionId, collectionId);
            }
        }

        public static async Task<T[]> ExecuteQueryAsync<T>(string sql, string collectionId, bool enableCrossPartitionQuery)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var query = Client.CreateDocumentQuery<T>(uri, sql, new FeedOptions { EnableCrossPartitionQuery = enableCrossPartitionQuery }).AsDocumentQuery();

            var results = new List<T>();
            while (query.HasMoreResults)
            {
                var items = await query.ExecuteNextAsync<T>();
                results.AddRange(items.AsEnumerable());
                RequestCharge += items.RequestCharge;
            }

            return results.ToArray();
        }

        public static async Task<T> ExecuteScalarQueryAsync<T>(string sql, string collectionId, bool enableCrossPartitionQuery)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var query = Client.CreateDocumentQuery<T>(uri, sql, new FeedOptions { EnableCrossPartitionQuery = enableCrossPartitionQuery }).AsDocumentQuery();

            var results = new List<T>();
            while (query.HasMoreResults)
            {
                var items = await query.ExecuteNextAsync<T>();
                results.AddRange(items.AsEnumerable());
                RequestCharge += items.RequestCharge;
            }

            return results[0];
        }

        public static void ResetRequestCharge()
        {
            RequestCharge = 0.0d;
        }

        public static string Diagnostics()
        {
            var results = $"Server name: {Environment.GetEnvironmentVariable("APPSETTING_WEBSITE_SITE_NAME") ?? "local"} <br/>";
            results += $"Region: {CurrentRegion} <br/>";
            results += $"Total RequestCharge: {RequestCharge:f2} <br/>";
            results += $"EndpointUrl: {EndpointUrl} <br/>";

            results += $"ServiceEndpoint: {Client.ServiceEndpoint} <br/>";
            results += $"ReadEndpoint: {Client.ReadEndpoint} <br/>";
            results += $"WriteEndpoint: {Client.WriteEndpoint} <br/>";
            results += $"ConsistencyLevel: {Client.ConsistencyLevel} <br/>";
            results += $"ConnectionMode: {Client.ConnectionPolicy.ConnectionMode} <br/>";
            results += $"ConnectionProtocol: {Client.ConnectionPolicy.ConnectionProtocol} <br/>";
            results += $"MaxConnectionLimit: {Client.ConnectionPolicy.MaxConnectionLimit} <br/>";
            results += $"PreferredLocations: {string.Join(", ", Client.ConnectionPolicy.PreferredLocations)} <br/>";

            return results;
        }
    }
}