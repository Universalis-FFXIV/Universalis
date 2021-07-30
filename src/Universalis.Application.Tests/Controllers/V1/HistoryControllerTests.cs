using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Controllers.V1;
using Universalis.Application.Tests.Mocks.DbAccess.MarketBoard;
using Universalis.Application.Tests.Mocks.GameData;
using Universalis.Application.Views;
using Universalis.DataTransformations;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class HistoryControllerTests
    {
        [Theory]
        [InlineData("74", "")]
        [InlineData("Coeurl", " bingus4645")]
        [InlineData("coEUrl", "50")]
        public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);
            var rand = new Random();

            var document = new History
            {
                WorldId = 74,
                ItemId = 5333,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document);

            var result = await controller.Get("5333", worldOrDc, entriesToReturn);
            var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

            AssertHistoryValidWorld(document, history, gameData, entriesToReturn);
        }

        [Theory]
        [InlineData("74", "")]
        [InlineData("Coeurl", " bingus4645")]
        [InlineData("coEUrl", "50")]
        public async Task Controller_Get_Succeeds_MultiItem_World(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);
            var rand = new Random();

            var document1 = new History
            {
                WorldId = 74,
                ItemId = 5333,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document1);

            var document2 = new History
            {
                WorldId = 74,
                ItemId = 5,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document2);

            var result = await controller.Get("5,5333", worldOrDc, entriesToReturn);
            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(5U, history.ItemIds);
            Assert.Contains(5333U, history.ItemIds);
            Assert.Empty(history.UnresolvedItemIds);
            Assert.Equal(2, history.Items.Count);
            Assert.Equal(74U, history.WorldId);
            Assert.Equal(gameData.AvailableWorlds()[74], history.WorldName);
            Assert.Null(history.DcName);

            AssertHistoryValidWorld(document1, history.Items.First(item => item.ItemId == document1.ItemId), gameData, entriesToReturn);
            AssertHistoryValidWorld(document2, history.Items.First(item => item.ItemId == document2.ItemId), gameData, entriesToReturn);
        }

        [Theory]
        [InlineData("crystaL", "")]
        [InlineData("Crystal", "50")]
        public async Task Controller_Get_Succeeds_SingleItem_DataCenter(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);
            var rand = new Random();

            var document1 = new History
            {
                WorldId = 74,
                ItemId = 5333,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document1);

            var document2 = new History
            {
                WorldId = 34,
                ItemId = 5333,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document2);

            var result = await controller.Get("5333", worldOrDc, entriesToReturn);
            var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

            var sales = document1.Sales.Concat(document2.Sales).ToList();
            var lastUploadTime = Math.Max(
                document1.LastUploadTimeUnixMilliseconds,
                document2.LastUploadTimeUnixMilliseconds);

            AssertHistoryValidDataCenter(
                document1,
                history,
                sales,
                lastUploadTime,
                worldOrDc,
                entriesToReturn);
        }

        [Theory]
        [InlineData("crystaL", "")]
        [InlineData("Crystal", "50")]
        public async Task Controller_Get_Succeeds_MultiItem_DataCenter(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);
            var rand = new Random();

            var document1 = new History
            {
                WorldId = 74,
                ItemId = 5333,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document1);

            var document2 = new History
            {
                WorldId = 34,
                ItemId = 5,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Sales = Enumerable.Range(0, 100)
                    .Select(i => new MinimizedSale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        SaleTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploaderIdHash = "2A",
                    })
                    .ToList(),
            };
            await dbAccess.Create(document2);

            var result = await controller.Get("5,5333", worldOrDc, entriesToReturn);
            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(5U, history.ItemIds);
            Assert.Contains(5333U, history.ItemIds);
            Assert.Empty(history.UnresolvedItemIds);
            Assert.Equal(2, history.Items.Count);
            Assert.Null(history.WorldId);
            Assert.Null(history.WorldName);
            Assert.Equal("Crystal", history.DcName);
            
            var lastUploadTime = Math.Max(
                document1.LastUploadTimeUnixMilliseconds,
                document2.LastUploadTimeUnixMilliseconds);

            AssertHistoryValidDataCenter(
                document1,
                history.Items.First(item => item.ItemId == document1.ItemId),
                document1.Sales,
                lastUploadTime,
                worldOrDc,
                entriesToReturn);
            AssertHistoryValidDataCenter(
                document2,
                history.Items.First(item => item.ItemId == document2.ItemId),
                document2.Sales,
                lastUploadTime,
                worldOrDc,
                entriesToReturn);
        }

        [Theory]
        [InlineData("74", "")]
        [InlineData("Coeurl", " bingus4645")]
        [InlineData("coEUrl", "50")]
        public async Task Controller_Get_Succeeds_SingleItem_World_WhenNone(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            const uint itemId = 5333;
            var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

            var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Equal(itemId, history.ItemId);
            Assert.Equal(74U, history.WorldId);
            Assert.Equal("Coeurl", history.WorldName);
            Assert.Null(history.DcName);
            Assert.NotNull(history.Sales);
            Assert.Empty(history.Sales);
            Assert.Equal(0U, history.LastUploadTimeUnixMilliseconds);
            Assert.NotNull(history.StackSizeHistogram);
            Assert.Empty(history.StackSizeHistogram);
            Assert.NotNull(history.StackSizeHistogramNq);
            Assert.Empty(history.StackSizeHistogramNq);
            Assert.NotNull(history.StackSizeHistogramHq);
            Assert.Empty(history.StackSizeHistogramHq);
            Assert.Equal(0, history.SaleVelocity);
            Assert.Equal(0, history.SaleVelocityNq);
            Assert.Equal(0, history.SaleVelocityHq);
        }

        [Theory]
        [InlineData("74", "")]
        [InlineData("Coeurl", " bingus4645")]
        [InlineData("coEUrl", "50")]
        public async Task Controller_Get_Succeeds_MultiItem_World_WhenNone(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            var result = await controller.Get("5333,5", worldOrDc, entriesToReturn);

            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(5U, history.UnresolvedItemIds);
            Assert.Contains(5333U, history.UnresolvedItemIds);
            Assert.Contains(5U, history.ItemIds);
            Assert.Contains(5333U, history.ItemIds);
            Assert.Empty(history.Items);
            Assert.Equal(74U, history.WorldId);
            Assert.Equal(gameData.AvailableWorlds()[74], history.WorldName);
            Assert.Null(history.DcName);
        }

        [Theory]
        [InlineData("crystaL", "")]
        [InlineData("Crystal", "50")]
        public async Task Controller_Get_Succeeds_SingleItem_DataCenter_WhenNone(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            const uint itemId = 5333;
            var result = await controller.Get(itemId.ToString(), worldOrDc, entriesToReturn);

            var history = (HistoryView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Equal(itemId, history.ItemId);
            Assert.Equal("Crystal", history.DcName);
            Assert.NotNull(history.Sales);
            Assert.Empty(history.Sales);
            Assert.Equal(0U, history.LastUploadTimeUnixMilliseconds);
            Assert.NotNull(history.StackSizeHistogram);
            Assert.Empty(history.StackSizeHistogram);
            Assert.NotNull(history.StackSizeHistogramNq);
            Assert.Empty(history.StackSizeHistogramNq);
            Assert.NotNull(history.StackSizeHistogramHq);
            Assert.Empty(history.StackSizeHistogramHq);
            Assert.Equal(0, history.SaleVelocity);
            Assert.Equal(0, history.SaleVelocityNq);
            Assert.Equal(0, history.SaleVelocityHq);
        }

        [Theory]
        [InlineData("crystaL", "")]
        [InlineData("Crystal", "50")]
        public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNone(string worldOrDc, string entriesToReturn)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            var result = await controller.Get("5333,5", worldOrDc, entriesToReturn);

            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(5U, history.UnresolvedItemIds);
            Assert.Contains(5333U, history.UnresolvedItemIds);
            Assert.Contains(5U, history.ItemIds);
            Assert.Contains(5333U, history.ItemIds);
            Assert.Empty(history.Items);
            Assert.Equal("Crystal", history.DcName);
            Assert.Null(history.WorldId);
        }

        [Fact]
        public async Task Controller_Get_Fails_SingleItem_World_WhenNotMarketable()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            const uint itemId = 0;
            var result = await controller.Get(itemId.ToString(), "74", "");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_MultiItem_World_WhenNotMarketable()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            var result = await controller.Get("0, 4294967295", "74", "");

            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(0U, history.UnresolvedItemIds);
            Assert.Contains(4294967295U, history.UnresolvedItemIds);
            Assert.Empty(history.Items);
            Assert.Equal(74U, history.WorldId);
            Assert.Null(history.DcName);
        }

        [Fact]
        public async Task Controller_Get_Fails_SingleItem__DataCenter_WhenNotMarketable()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            const uint itemId = 0;
            var result = await controller.Get(itemId.ToString(), "Crystal", "");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Controller_Get_Succeeds_MultiItem_DataCenter_WhenNotMarketable()
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockHistoryDbAccess();
            var controller = new HistoryController(gameData, dbAccess);

            var result = await controller.Get("0 ,4294967295", "crystal", "");

            var history = (HistoryMultiView)Assert.IsType<OkObjectResult>(result).Value;

            Assert.Contains(0U, history.UnresolvedItemIds);
            Assert.Contains(4294967295U, history.UnresolvedItemIds);
            Assert.Contains(0U, history.ItemIds);
            Assert.Contains(4294967295U, history.ItemIds);
            Assert.Empty(history.Items);
            Assert.Equal("Crystal", history.DcName);
            Assert.Null(history.WorldId);
        }

        private static void AssertHistoryValidWorld(History document, HistoryView history, IGameDataProvider gameData, string entriesToReturn)
        {
            document.Sales.Sort((a, b) => (int)b.SaleTimeUnixSeconds - (int)a.SaleTimeUnixSeconds);
            if (int.TryParse(entriesToReturn, out var entries))
            {
                entries = Math.Max(0, entries);
                Assert.True(history.Sales.Count <= entries);
                document.Sales = document.Sales.Take(entries).ToList();
            }

            var nqSales = document.Sales.Where(s => !s.Hq).ToList();
            var hqSales = document.Sales.Where(s => s.Hq).ToList();

            Assert.Equal(document.ItemId, history.ItemId);
            Assert.Equal(document.WorldId, history.WorldId);
            Assert.Equal(gameData.AvailableWorlds()[document.WorldId], history.WorldName);
            Assert.Null(history.DcName);
            Assert.NotNull(history.Sales);
            Assert.Equal(document.LastUploadTimeUnixMilliseconds, history.LastUploadTimeUnixMilliseconds);

            Assert.Equal(Statistics.GetDistribution(document.Sales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogram);
            Assert.Equal(Statistics.GetDistribution(nqSales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogramNq);
            Assert.Equal(Statistics.GetDistribution(hqSales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogramHq);
            Assert.Equal(Statistics.WeekVelocityPerDay(document.Sales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocity);
            Assert.Equal(Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocityNq);
            Assert.Equal(Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocityHq);
        }

        private static void AssertHistoryValidDataCenter(History anyWorldDocument, HistoryView history, List<MinimizedSale> sales, uint lastUploadTime, string worldOrDc, string entriesToReturn)
        {
            sales.Sort((a, b) => (int)b.SaleTimeUnixSeconds - (int)a.SaleTimeUnixSeconds);
            if (int.TryParse(entriesToReturn, out var entries))
            {
                entries = Math.Max(0, entries);
                Assert.True(history.Sales.Count <= entries);
                sales = sales.Take(entries).ToList();
            }

            var nqSales = sales.Where(s => !s.Hq).ToList();
            var hqSales = sales.Where(s => s.Hq).ToList();

            Assert.Contains(history.Sales, s => s.WorldId != null);
            Assert.Contains(history.Sales, s => s.WorldName != null);

            Assert.Equal(anyWorldDocument.ItemId, history.ItemId);
            Assert.Equal(char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant(), history.DcName);
            Assert.Null(history.WorldId);
            Assert.Null(history.WorldName);
            Assert.NotNull(history.Sales);
            Assert.Equal(lastUploadTime, history.LastUploadTimeUnixMilliseconds);

            Assert.Equal(Statistics.GetDistribution(sales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogram);
            Assert.Equal(Statistics.GetDistribution(nqSales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogramNq);
            Assert.Equal(Statistics.GetDistribution(hqSales
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                history.StackSizeHistogramHq);
            Assert.Equal(Statistics.WeekVelocityPerDay(sales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocity);
            Assert.Equal(Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocityNq);
            Assert.Equal(Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                history.SaleVelocityHq);
        }
    }
}