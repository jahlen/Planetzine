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

        public static async Task CreateDocument(object document, string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var response = await Client.CreateDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task UpsertDocument(object document, string collectionId)
        {
            var uri = UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId);
            var response = await Client.UpsertDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task ReplaceDocument(object document, string documentId, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);
            var response = await Client.ReplaceDocumentAsync(uri, document);
            RequestCharge += response.RequestCharge;
        }

        public static async Task<DocumentResponse<T>> GetDocument<T>(string documentId, object partitionKey, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);

            var response = await Client.ReadDocumentAsync<T>(uri, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            RequestCharge += response.RequestCharge;

            return response;
        }

        public static async Task DeleteDocument(string documentId, object partitionKey, string collectionId)
        {
            var uri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, documentId);

            var response = await Client.DeleteDocumentAsync(uri, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
            RequestCharge += response.RequestCharge;
        }

        public static async Task<T[]> ExecuteQuery<T>(string sql, string collectionId, bool enableCrossPartitionQuery)
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

        public static async Task<T> ExecuteScalarQuery<T>(string sql, string collectionId, bool enableCrossPartitionQuery)
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
            var results = $"Server name: {ServerName} <br/>";
            results += $"Server suffix: {ServerSuffix} <br/>";
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