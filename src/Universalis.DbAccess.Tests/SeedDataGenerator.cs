using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Entities;
using Universalis.Entities.AccessControl;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Tests;

public static class SeedDataGenerator
{
    public static CurrentlyShown MakeCurrentlyShown(int worldId, int itemId, long? lastUploadTime = null, int maxStackSize = 999)
    {
        var rand = new Random();
        var usedPrices = new List<int>();
        var t = lastUploadTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var listings = Enumerable.Range(0, 100)
            .Select(_ =>
            {
                var price = rand.Next(100, 60000);
                while (usedPrices.Contains(price))
                {
                    price += 1;
                }
                usedPrices.Add(price);

                return new Listing
                {
                    ListingId = rand.NextInt64().ToString(),
                    Hq = rand.NextDouble() > 0.5,
                    OnMannequin = rand.NextDouble() > 0.5,
                    Materia = new List<Materia>(),
                    PricePerUnit = price,
                    Quantity = rand.Next(1, maxStackSize),
                    DyeId = (byte)rand.Next(0, 255),
                    CreatorId = rand.NextInt64().ToString(),
                    CreatorName = "Bingus Bongus",
                    LastReviewTime = DateTime.UtcNow - TimeSpan.FromSeconds(rand.Next(0, 360000)),
                    RetainerId = rand.NextInt64().ToString(),
                    RetainerName = "xpotato",
                    RetainerCityId = 0xA,
                    SellerId = rand.NextInt64().ToString(),
                    ItemId = itemId,
                    WorldId = worldId,
                    Source = "test runner",
                };
            })
            .ToList();
        return new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = t,
            UploadSource = "test runner",
            Listings = listings,
        };
    }

    public static History MakeHistory(int worldId, int itemId, long? lastUploadTime = null, int? maxStackSize = 999)
    {
        var rand = new Random();
        return new History
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = lastUploadTime ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Sales = Enumerable.Range(0, 100)
                .Select(_ => new Sale
                {
                    Id = Guid.NewGuid(),
                    WorldId = worldId,
                    ItemId = itemId,
                    Hq = rand.NextDouble() > 0.5,
                    PricePerUnit = rand.Next(100, 60000),
                    Quantity = rand.Next(1, (int)maxStackSize),
                    SaleTime = DateTime.UtcNow - new TimeSpan(rand.Next(0, 2100000000)),
                    UploaderIdHash = "2A",
                })
                .ToList(),
        };
    }
    
    public static TaxRates MakeTaxRates(int worldId)
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
        return new FlaggedUploader("afffff");
    }

    public static ApiKey MakeApiKey()
    {
        return new ApiKey("aefe32ee", "test runner", true);
    }
}