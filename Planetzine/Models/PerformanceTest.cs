using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

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

            public long OperationPerSecond => ElapsedMilliseconds != 0 ? NumberOfOperations * 1000 / ElapsedMilliseconds : 0;
            public double RUsPerSecond => ElapsedMilliseconds != 0 ? RUCost * 1000 / ElapsedMilliseconds : 0;
            public double RUsPerDocument => NumberOfOperations != 0 ? RUCost / NumberOfOperations : 0;
        };

        // Input parameters for the test
        public int NumberOfWritesPrimary { get; set; }
        public int NumberOfWritesSecondary { get; set; }
        public int NumberOfQueryResultsPrimary { get; set; }
        public int NumberOfQueryResultsSecondary { get; set; }
        public int NumberOfRandomReadsPrimary { get; set; }
        public int NumberOfRandomReadsSecondary { get; set; }
        public int NumberOfUpsertsPrimary { get; set; }
        public int NumberOfUpsertsSecondary { get; set; }
        public int Parallelism { get; set; }

        // The clients used (could be the same if there is only one client available)
 /*       public DbClientInfo PrimaryClient;
        public DbClientInfo SecondaryClient;

        // Test results
        public Results WritesPrimary;
        public Results WritesSecondary;
        public Results QueryPrimary;
        public Results QuerySecondary;
        public Results RandomReadsPrimary;
        public Results RandomReadsSecondary;
        public Results UpsertsPrimary;
        public Results UpsertsSecondary;

        public Results[] AllResults => new[] { WritesPrimary, WritesSecondary, QueryPrimary, QuerySecondary, RandomReadsPrimary, RandomReadsSecondary, UpsertsPrimary, UpsertsSecondary };

        private int counter;
        private Player[] playersPrimary;
        private Player[] playersSecondary;

        public async Task RunTests()
        {
            if (Parallelism < 1)
                Parallelism = 1;
            if (NumberOfQueryResultsPrimary < 1)
                NumberOfQueryResultsPrimary = 1;
            if (NumberOfQueryResultsSecondary < 1)
                NumberOfQueryResultsSecondary = 1;

            PrimaryClient = DbHelper.PrimaryClient;
            if (DbHelper.Clients.Length == 1)
            {
                // If there is only one client available, use also as secondary
                SecondaryClient = PrimaryClient;
            }
            else
            {
                // Make sure we don't take the primary client
                SecondaryClient = DbHelper.Clients.Where(c => c != PrimaryClient).Last();
            }

            WritesPrimary = await RunCreateTest("WritesPrimary", PrimaryClient, NumberOfWritesPrimary);
            WritesSecondary = await RunCreateTest("WritesSecondary", SecondaryClient, NumberOfWritesSecondary);

            QueryPrimary = await RunTest("QueryPrimary", async () => playersPrimary = await DbHelper.Query<Player>(PrimaryClient, $"TOP {NumberOfQueryResultsPrimary}", null, Player.CollectionId), NumberOfQueryResultsPrimary);
            QuerySecondary = await RunTest("QuerySecondary", async () => playersSecondary = await DbHelper.Query<Player>(SecondaryClient, $"TOP {NumberOfQueryResultsSecondary}", null, Player.CollectionId), NumberOfQueryResultsSecondary);

            RandomReadsPrimary = await RunRandomReadTest("RandomReadsPrimary", PrimaryClient, NumberOfRandomReadsPrimary, playersPrimary);
            RandomReadsSecondary = await RunRandomReadTest("RandomReadsSecondary", SecondaryClient, NumberOfRandomReadsSecondary, playersSecondary);

            UpsertsPrimary = await RunUpsertTest("UpsertsPrimary", PrimaryClient, NumberOfUpsertsPrimary, playersPrimary);
            UpsertsSecondary = await RunUpsertTest("UpsertsSecondary", SecondaryClient, NumberOfUpsertsSecondary, playersSecondary);
        }

        private async Task<Results> RunTest(string testName, Func<Task> func, int count)
        {
            var stopWatch = Stopwatch.StartNew();
            var prevRequestCharge = DbHelper.RequestCharge;
            await func();
            var results = new Results { ElapsedMilliseconds = stopWatch.ElapsedMilliseconds, RUCost = DbHelper.RequestCharge - prevRequestCharge, Name = testName, NumberOfOperations = count };
            return results;
        }

        private async Task<Results> RunCreateTest(string testName, DbClientInfo client, int count)
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

                        var player = Player.New();
                        player.ClientName = client.Name;
                        await DbHelper.Create(player, Player.CollectionId);
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }

        private async Task<Results> RunRandomReadTest(string testName, DbClientInfo client, int count, Player[] players)
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
                        var j = new Random(i).Next(players.Length);
                        var player = await Player.Load(players[j].PlayerGuid, players[j].ClientName);
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }

        private async Task<Results> RunUpsertTest(string testName, DbClientInfo client, int count, Player[] players)
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
                        var j = new Random(i).Next(players.Length);
                        players[j].Balance += 5;
                        await players[j].Upsert();
                    }
                }
                catch (DocumentClientException ex)
                {
                    // If we get any DocumentClientException (for instance a RequestRateTooLargeException) - quit this task
                    // Maybe should notify the user?
                }
            }
        }*/
    }
}