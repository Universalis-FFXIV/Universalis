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
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;
using Xunit;

namespace Universalis.Application.Tests.Controllers.V1
{
    public class CurrentlyShownControllerTests
    {
        [Theory]
        [InlineData("74")]
        [InlineData("Coeurl")]
        [InlineData("coEUrl")]
        public async Task Controller_Get_Succeeds_SingleItem_World(string worldOrDc)
        {
            var gameData = new MockGameDataProvider();
            var dbAccess = new MockCurrentlyShownDbAccess();
            var controller = new CurrentlyShownController(gameData, dbAccess);
            var rand = new Random();

            const uint itemId = 5333;
            var document = new CurrentlyShown
            {
                WorldId = 74,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Listings = Enumerable.Range(0, 100)
                    .Select(i => new Listing
                    {
                        ListingId = "FB",
                        Hq = rand.NextDouble() > 0.5,
                        OnMannequin = rand.NextDouble() > 0.5,
                        Materia = new List<Materia>(),
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        DyeId = (byte)rand.Next(0, 255),
                        CreatorIdHash = "3a5f66de",
                        CreatorName = "Bingus Bongus",
                        LastReviewTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 360000),
                        RetainerId = "54565458626446136554",
                        RetainerName = "xpotato",
                        RetainerCityId = 0xA,
                        SellerIdHash = "3a5f66de",
                        UploadApplicationName = "test runner",
                    })
                    .ToList(),
                RecentHistory = Enumerable.Range(0, 100)
                    .Select(i => new Sale
                    {
                        Hq = rand.NextDouble() > 0.5,
                        PricePerUnit = (uint)rand.Next(100, 60000),
                        Quantity = (uint)rand.Next(1, 999),
                        BuyerName = "Someone Someone",
                        TimestampUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                        UploadApplicationName = "test runner",
                    })
                    .ToList(),
                UploaderIdHash = "2A",
            };
            await dbAccess.Create(document);

            var result = await controller.Get(itemId.ToString(), worldOrDc);
            var currentlyShown = (CurrentlyShownView)Assert.IsType<OkObjectResult>(result).Value;

            AssertCurrentlyShownValidWorld(document, currentlyShown, gameData);
        }

        private static void AssertCurrentlyShownValidWorld(CurrentlyShown document, CurrentlyShownView currentlyShown, IGameDataProvider gameData)
        {
            Assert.Equal(document.ItemId, currentlyShown.ItemId);
            Assert.Equal(document.WorldId, currentlyShown.WorldId);
            Assert.Equal(gameData.AvailableWorlds()[document.WorldId], currentlyShown.WorldName);
            Assert.Equal(document.LastUploadTimeUnixMilliseconds, currentlyShown.LastUploadTimeUnixMilliseconds);
            Assert.Null(currentlyShown.DcName);

            Assert.NotNull(currentlyShown.Listings);
            Assert.NotNull(currentlyShown.RecentHistory);

            currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
            currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

            Assert.All(currentlyShown.Listings.Select(l => (object)l.WorldId), Assert.Null);
            Assert.All(currentlyShown.Listings.Select(l => l.WorldName), Assert.Null);

            Assert.All(currentlyShown.RecentHistory.Select(s => (object)s.WorldId), Assert.Null);
            Assert.All(currentlyShown.RecentHistory.Select(s => s.WorldName), Assert.Null);

            var nqListings = currentlyShown.Listings.Where(s => !s.Hq).ToList();
            var hqListings = currentlyShown.Listings.Where(s => s.Hq).ToList();

            var nqHistory = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
            var hqHistory = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();

            var currentAveragePrice = Filters.RemoveOutliers(currentlyShown.Listings.Select(s => (float)s.PricePerUnit), 3).Average();
            var currentAveragePriceNq = Filters.RemoveOutliers(nqListings.Select(s => (float)s.PricePerUnit), 3).Average();
            var currentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(s => (float)s.PricePerUnit), 3).Average();

            Assert.Equal(Round(currentAveragePrice), Round(currentlyShown.CurrentAveragePrice));
            Assert.Equal(Round(currentAveragePriceNq), Round(currentlyShown.CurrentAveragePriceNq));
            Assert.Equal(Round(currentAveragePriceHq), Round(currentlyShown.CurrentAveragePriceHq));

            var averagePrice = Filters.RemoveOutliers(currentlyShown.RecentHistory.Select(s => (float)s.PricePerUnit), 3).Average();
            var averagePriceNq = Filters.RemoveOutliers(nqHistory.Select(s => (float)s.PricePerUnit), 3).Average();
            var averagePriceHq = Filters.RemoveOutliers(hqHistory.Select(s => (float)s.PricePerUnit), 3).Average();

            Assert.Equal(Round(averagePrice), Round(currentlyShown.AveragePrice));
            Assert.Equal(Round(averagePriceNq), Round(currentlyShown.AveragePriceNq));
            Assert.Equal(Round(averagePriceHq), Round(currentlyShown.AveragePriceHq));

            var minPrice = currentlyShown.Listings.Min(l => l.PricePerUnit);
            var minPriceNq = nqListings.Min(l => l.PricePerUnit);
            var minPriceHq = hqListings.Min(l => l.PricePerUnit);

            Assert.Equal(minPrice, currentlyShown.MinPrice);
            Assert.Equal(minPriceNq, currentlyShown.MinPriceNq);
            Assert.Equal(minPriceHq, currentlyShown.MinPriceHq);

            var maxPrice = currentlyShown.Listings.Max(l => l.PricePerUnit);
            var maxPriceNq = nqListings.Max(l => l.PricePerUnit);
            var maxPriceHq = hqListings.Max(l => l.PricePerUnit);

            Assert.Equal(maxPrice, currentlyShown.MaxPrice);
            Assert.Equal(maxPriceNq, currentlyShown.MaxPriceNq);
            Assert.Equal(maxPriceHq, currentlyShown.MaxPriceHq);

            var saleVelocity = Statistics.WeekVelocityPerDay(
                currentlyShown.RecentHistory.Select(s => (long)s.TimestampUnixSeconds * 1000));
            var saleVelocityNq = Statistics.WeekVelocityPerDay(
                nqHistory.Select(s => (long)s.TimestampUnixSeconds * 1000));
            var saleVelocityHq = Statistics.WeekVelocityPerDay(
                hqHistory.Select(s => (long)s.TimestampUnixSeconds * 1000));

            Assert.Equal(Round(saleVelocity), Round(currentlyShown.SaleVelocity));
            Assert.Equal(Round(saleVelocityNq), Round(currentlyShown.SaleVelocityNq));
            Assert.Equal(Round(saleVelocityHq), Round(currentlyShown.SaleVelocityHq));

            var stackSizeHistogram = Statistics.GetDistribution(currentlyShown.Listings.Select(l => (int)l.Quantity));
            var stackSizeHistogramNq = Statistics.GetDistribution(nqListings.Select(l => (int)l.Quantity));
            var stackSizeHistogramHq = Statistics.GetDistribution(hqListings.Select(l => (int)l.Quantity));

            Assert.Equal(stackSizeHistogram, currentlyShown.StackSizeHistogram);
            Assert.Equal(stackSizeHistogramNq, currentlyShown.StackSizeHistogramNq);
            Assert.Equal(stackSizeHistogramHq, currentlyShown.StackSizeHistogramHq);
        }

        private static double Round(double value)
        {
            return Math.Round(value, 3);
        }
    }
}