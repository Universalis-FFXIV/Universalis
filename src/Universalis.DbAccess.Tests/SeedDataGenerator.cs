using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.Tests
{
    public static class SeedDataGenerator
    {
        public static CurrentlyShown MakeCurrentlyShown(uint worldId, uint itemId, uint? lastUploadTime = null)
        {
            var rand = new Random();
            return new CurrentlyShown
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = lastUploadTime ?? (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
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
        }

        public static History MakeHistory(uint worldId, uint itemId, uint? lastUploadTime = null)
        {
            var rand = new Random();
            return new History
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = lastUploadTime ?? (uint)DateTimeOffset.Now.ToUnixTimeMilliseconds(),
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
        }

        public static TaxRates MakeTaxRates(uint worldId)
        {
            return new()
            {
                WorldId = worldId,
                UploaderIdHash = "",
                UploadApplicationName = "test runner",
                LimsaLominsa = 3,
                Gridania = 3,
                Uldah = 3,
                Ishgard = 0,
                Kugane = 0,
                Crystarium = 5,
            };
        }
    }
}