using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Tests;

public static class SeedDataGenerator
{
    public static CurrentlyShown MakeCurrentlyShown(uint worldId, uint itemId, long? lastUploadTime = null, uint maxStackSize = 999)
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
                    ListingIdInternal = (ulong)rand.NextInt64(),
                    Hq = rand.NextDouble() > 0.5,
                    OnMannequin = rand.NextDouble() > 0.5,
                    Materia = new List<Materia>(),
                    PricePerUnit = (uint)rand.Next(100, 60000),
                    Quantity = (uint)rand.Next(1, (int)maxStackSize),
                    DyeId = (byte)rand.Next(0, 255),
                    CreatorIdInternal = (ulong)rand.NextInt64(),
                    CreatorName = "Bingus Bongus",
                    LastReviewTimeUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 360000),
                    RetainerIdInternal = (ulong)rand.NextInt64(),
                    RetainerName = "xpotato",
                    RetainerCityIdInternal = 0xA,
                    SellerIdInternal = (ulong)rand.NextInt64(),
                    UploadApplicationName = "test runner",
                })
                .ToList(),
            RecentHistory = Enumerable.Range(0, 100)
                .Select(i => new Sale
                {
                    Hq = rand.NextDouble() > 0.5,
                    PricePerUnit = (uint)rand.Next(100, 60000),
                    Quantity = (uint)rand.Next(1, (int)maxStackSize),
                    BuyerName = "Someone Someone",
                    TimestampUnixSeconds = (uint)DateTimeOffset.Now.ToUnixTimeSeconds() - (uint)rand.Next(0, 80000),
                    UploadApplicationName = "test runner",
                })
                .ToList(),
            UploaderIdHash = "2A",
        };
    }

    public static History MakeHistory(uint worldId, uint itemId, long? lastUploadTime = null)
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
    
    public static TaxRatesSimple MakeTaxRatesSimple(uint worldId)
    {
        return new()
        {
            UploadApplicationName = "test runner",
            LimsaLominsa = 3,
            Gridania = 3,
            Uldah = 3,
            Ishgard = 0,
            Kugane = 0,
            Crystarium = 5,
        };
    }

    public static FlaggedUploader MakeFlaggedUploader()
    {
        return new() { UploaderIdHash = "afffff" };
    }

    public static TrustedSource MakeTrustedSource()
    {
        return new()
        {
            ApiKeySha512 = "aefe32ee",
            Name = "test runner",
            UploadCount = 0,
        };
    }
}