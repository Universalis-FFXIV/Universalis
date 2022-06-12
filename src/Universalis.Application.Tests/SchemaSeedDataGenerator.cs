using System;
using System.Collections.Generic;
using System.Linq;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Tests;

namespace Universalis.Application.Tests;

public static class SchemaSeedDataGenerator
{
    public static (List<Listing>, List<Sale>) GetUploadListingsAndSales(uint worldId, uint itemId, uint maxStackSize = 999)
    {
        var seed = SeedDataGenerator.MakeCurrentlyShown(worldId, itemId, maxStackSize: maxStackSize);
        var seedHistory = SeedDataGenerator.MakeHistory(worldId, itemId, maxStackSize: maxStackSize);

        var listings = seed.Listings
            .Select(l => new Listing
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
                SellerId = l.SellerId,
                CreatorId = l.CreatorId,
                DyeId = l.DyeId,
                LastReviewTimeUnixSeconds = Convert.ToInt64(l.LastReviewTimeUnixSeconds),
                Materia = l.Materia
                    .Select(m => new Materia
                    {
                        SlotId = m.SlotId,
                        MateriaId = m.MateriaId,
                    })
                    .ToList(),
            })
            .ToList();

        var sales = seedHistory.Sales
            .Select(s => new Sale
            {
                BuyerId = "",
                BuyerName = s.BuyerName,
                Hq = s.Hq.ToString(),
                OnMannequin = false.ToString(),
                PricePerUnit = s.PricePerUnit,
                Quantity = s.Quantity,
                SellerId = "",
                TimestampUnixSeconds = new DateTimeOffset(s.SaleTime).ToUnixTimeSeconds(),
            })
            .ToList();

        return (listings, sales);
    }
}