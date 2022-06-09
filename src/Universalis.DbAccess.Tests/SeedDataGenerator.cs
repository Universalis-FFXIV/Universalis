﻿using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Tests;

public static class SeedDataGenerator
{
    public static CurrentlyShown MakeCurrentlyShownSimple(uint worldId, uint itemId, long? lastUploadTime = null, uint maxStackSize = 999)
    {
        var rand = new Random();
        var t = lastUploadTime ?? DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var listings = Enumerable.Range(0, 100)
            .Select(i => new Listing
            {
                ListingId = rand.NextInt64().ToString(),
                Hq = rand.NextDouble() > 0.5,
                OnMannequin = rand.NextDouble() > 0.5,
                Materia = new List<Materia>(),
                PricePerUnit = (uint)rand.Next(100, 60000),
                Quantity = (uint)rand.Next(1, (int)maxStackSize),
                DyeId = (byte)rand.Next(0, 255),
                CreatorId = rand.NextInt64().ToString(),
                CreatorName = "Bingus Bongus",
                LastReviewTimeUnixSeconds = (uint)(DateTimeOffset.Now.ToUnixTimeSeconds() - rand.Next(0, 360000)),
                RetainerId = rand.NextInt64().ToString(),
                RetainerName = "xpotato",
                RetainerCityId = 0xA,
                SellerId = rand.NextInt64().ToString(),
            })
            .ToList();
        var sales = Enumerable.Range(0, 100)
            .Select(i => new Sale
            {
                Hq = rand.NextDouble() > 0.5,
                PricePerUnit = (uint)rand.Next(100, 60000),
                Quantity = (uint)rand.Next(1, (int)maxStackSize),
                BuyerName = "Someone Someone",
                SaleTime = DateTimeOffset.UtcNow - new TimeSpan(rand.Next(0, 80000)),
            })
            .ToList();
        return new CurrentlyShown(worldId, itemId, t, "test runner", listings, sales);
    }

    public static History MakeHistory(uint worldId, uint itemId, long? lastUploadTime = null)
    {
        var rand = new Random();
        return new History
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = lastUploadTime ?? DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            Sales = Enumerable.Range(0, 100)
                .Select(i => new Sale
                {
                    Hq = rand.NextDouble() > 0.5,
                    PricePerUnit = (uint)rand.Next(100, 60000),
                    Quantity = (uint)rand.Next(1, 999),
                    SaleTime = DateTimeOffset.UtcNow - new TimeSpan(rand.Next(0, 80000)),
                    UploaderIdHash = "2A",
                })
                .ToList(),
        };
    }
    
    public static TaxRates MakeTaxRates(uint worldId)
    {
        return new TaxRates
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
        return new FlaggedUploader { UploaderIdHash = "afffff" };
    }

    public static TrustedSource MakeTrustedSource()
    {
        return new TrustedSource
        {
            ApiKeySha512 = "aefe32ee",
            Name = "test runner",
            UploadCount = 0,
        };
    }
}