using System.Collections.Generic;
using System.Linq;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Tests;

namespace Universalis.Application.Tests
{
    public static class SchemaSeedDataGenerator
    {
        public static (List<Listing>, List<Sale>) GetUploadListingsAndSales(uint worldId, uint itemId)
        {
            var seed = SeedDataGenerator.MakeCurrentlyShown(worldId, itemId);

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
                    SellerId = l.SellerIdHash,
                    CreatorId = l.CreatorIdHash,
                    DyeId = l.DyeId,
                    LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds,
                    Materia = l.Materia
                        .Select(m => new Materia
                        {
                            SlotId = m.SlotId,
                            MateriaId = m.MateriaId,
                        })
                        .ToList(),
                })
                .ToList();

            var sales = seed.RecentHistory
                .Select(s => new Sale
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