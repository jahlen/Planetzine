using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Planetzine.Common;

namespace Planetzine.Models
{
    public class PerformanceTest
    {
        public struct Results
        {
            public string Name;
            public long ElapsedMilliseconds;
            public double RUCost;
            public int NumberOfOperations;

            public long DocumentsPerSecond => ElapsedMilliseconds != 0 ? NumberOfOperations * 1000 / ElapsedMilliseconds : 0;
            public double RUsPerSecond => ElapsedMilliseconds != 0 ? RUCost * 1000 / ElapsedMilliseconds : 0;
            public double RUsPerDocument => NumberOfOperations != 0 ? RUCost / NumberOfOperations : 0;
        };

        // Input parameters for the test
        public int NumberOfWrites { get; set; }
        public int NumberOfQueryResults { get; set; }
        public int NumberOfRandomReads { get; set; }
        public int NumberOfUpserts { get; set; }
        public int Parallelism { get; set; }

        // Test results
        public Results Writes;
        public Results Query;
        public Results RandomReads;
        public Results Upserts;

        public Results[] AllResults => new[] { Writes, Query, RandomReads, Upserts };

        private int counter;
        private Article[] articles;
        private Random random = new Random();

        public async Task RunTests()
        {
            if (Parallelism < 1)
                Parallelism = 1;
            if (NumberOfQueryResults < 1)
                NumberOfQueryResults = 1;

            Writes = await RunCreateTest("Writes", NumberOfWrites);
            Query = await RunTest("Query", async () => articles = await DbHelper.ExecuteQueryAsync<Article>($"SELECT TOP {NumberOfQueryResults} * FROM {Article.CollectionId} AS c", Article.CollectionId, true), NumberOfQueryResults);
            RandomReads = await RunRandomReadTest("RandomReads", NumberOfRandomReads, articles);
            Upserts = await RunUpsertTest("Upserts", NumberOfUpserts, articles);
        }

        private async Task<Results> RunTest(string testName, Func<Task> func, int count)
        {
            var stopWatch = Stopwatch.StartNew();
            var prevRequestCharge = DbHelper.RequestCharge;
            await func();
            var results = new Results { ElapsedMilliseconds = stopWatch.ElapsedMilliseconds, RUCost = DbHelper.RequestCharge - prevRequestCharge, Name = testName, NumberOfOperations = count };
            return results;
        }

        private async Task<Results> RunCreateTest(string testName, int count)
        {
            var stopWatch = Stopwatch.StartNew();
            var prevRequestCharge = DbHelper.RequestCharge;
            counter = 0;
            var tasks = Enumerable.Range(0, Parallelism).Select(i => Task.Run(Create)).ToArray();
            await Task.WhenAll(tasks);
            var results = new Results { ElapsedMilliseconds = stopWatch.ElapsedMilliseconds, RUCost = DbHelper.RequestCharge - prevRequestCharge, Name = testName, NumberOfOperations = count };
            return results;

            async Task Create()
            {
                try
                {
                    while (true)
                    {
                        var i = System.Threading.Interlocked.Increment(ref counter);
                        if (i > count)
                            return;

                        var article = Article.New();
                        article.ArticleId = Guid.NewGuid();
                        article.Body = GetRandomString(1000);
                        article.Author = GetRandomString(20);
                        await article.Create();
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }

        private async Task<Results> RunRandomReadTest(string testName, int count, Article[] articles)
        {
            var stopWatch = Stopwatch.StartNew();
            var prevRequestCharge = DbHelper.RequestCharge;
            counter = 0;
            var tasks = Enumerable.Range(0, Parallelism).Select(i => Task.Run(Read)).ToArray();
            await Task.WhenAll(tasks);
            var results = new Results { ElapsedMilliseconds = stopWatch.ElapsedMilliseconds, RUCost = DbHelper.RequestCharge - prevRequestCharge, Name = testName, NumberOfOperations = count };
            return results;

            async Task Read()
            {
                try
                {
                    while (true)
                    {
                        var i = System.Threading.Interlocked.Increment(ref counter);
                        if (i > count)
                            return;
                        var j = new Random(i).Next(articles.Length);
                        var article = await Article.Read(articles[j].ArticleId, articles[j].PartitionId);
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }

        private async Task<Results> RunUpsertTest(string testName, int count, Article[] articles)
        {
            var stopWatch = Stopwatch.StartNew();
            var prevRequestCharge = DbHelper.RequestCharge;
            counter = 0;
            var tasks = Enumerable.Range(0, Parallelism).Select(i => Task.Run(Upsert)).ToArray();
            await Task.WhenAll(tasks);
            var results = new Results { ElapsedMilliseconds = stopWatch.ElapsedMilliseconds, RUCost = DbHelper.RequestCharge - prevRequestCharge, Name = testName, NumberOfOperations = count };
            return results;

            async Task Upsert()
            {
                try
                {
                    while (true)
                    {
                        var i = System.Threading.Interlocked.Increment(ref counter);
                        if (i > count)
                            return;
                        var j = new Random(i).Next(articles.Length);
                        articles[j].LastUpdate = DateTime.Now;
                        await articles[j].Upsert();
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }

        private string GetRandomString(int length)
        {
            var sb = new StringBuilder(length);
            sb.Length = length;
            for (var i = 0; i < length; i++)
                sb[i] = (char)(32 + random.Next(128 - 32));

            return sb.ToString();
        }
    }
}