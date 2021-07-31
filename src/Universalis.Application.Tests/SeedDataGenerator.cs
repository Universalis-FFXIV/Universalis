using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Tests
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

        public static (List<Application.Uploads.Schema.Listing>, List<Application.Uploads.Schema.Sale>) GetUploadListingsAndSales(uint worldId, uint itemId)
        {
            var seed = MakeCurrentlyShown(worldId, itemId);

            var listings = seed.Listings
                .Select(l => new Application.Uploads.Schema.Listing
                {
                    ListingId = l.ListingId,
                    Hq = l.Hq.ToString(),
                    PricePerUnit = l.PricePerUnit,
                    Quantity = l.Quantity,
                    RetainerName = l.RetainerName,
                    RetainerId = l.RetainerId,
                    RetainerCityId = l.RetainerCityId,
                    CreatorName = l.CreatorName,
                    OnMannequin = l.OnMannequin.ToString(),
                    SellerId = l.SellerIdHash,
                    CreatorId = l.CreatorIdHash,
                    DyeId = l.DyeId,
                    LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds,
                    Materia = l.Materia
                        .Select(m => new Application.Uploads.Schema.Materia
                        {
                            SlotId = m.SlotId,
                            MateriaId = m.MateriaId,
                        })
                        .ToList(),
                })
                .ToList();

            var sales = seed.RecentHistory
                .Select(s => new Application.Uploads.Schema.Sale
                {
                    BuyerId = "",
                    BuyerName = s.BuyerName,
                    Hq = s.Hq.ToString(),
                    OnMannequin = false.ToString(),
                    PricePerUnit = s.PricePerUnit,
                    Quantity = s.Quantity,
                    SellerId = "",
                    TimestampUnixSeconds = s.TimestampUnixSeconds,
                })
                .ToList();

            return (listings, sales);
        }
    }
}