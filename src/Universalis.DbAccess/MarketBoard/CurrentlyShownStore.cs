using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly IConnectionMultiplexer _redis;

    public CurrentlyShownStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<CurrentlyShownSimple> GetData(uint worldId, uint itemId)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);

        if (!await db.KeyExistsAsync(lastUpdatedKey))
        {
            return new CurrentlyShownSimple(0, 0, 0, "",
                new List<ListingSimple>(), new List<SaleSimple>());
        }
        
        // Fetch all of the data in a consistent manner. This shouldn't usually run more
        // than once, given that reads are far more frequent than writes.
        bool transactionExecuted;
        long lastUpdated;
        string source;
        List<ListingSimple> listings;
        List<SaleSimple> sales;
        do
        {
            lastUpdated = await EnsureLastUpdated(worldId, itemId);
            
            var trans = db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(lastUpdatedKey, lastUpdated));

            source = await GetSource(db, worldId, itemId);
            listings = await GetListings(db, worldId, itemId);
            sales = await GetSales(db, worldId, itemId);

            transactionExecuted = await trans.ExecuteAsync();
        } while (!transactionExecuted);
        
        return new CurrentlyShownSimple(worldId, itemId, lastUpdated, source, listings, sales);
    }

    public async Task SetData(CurrentlyShownSimple data)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);

        var worldId = data.WorldId;
        var itemId = data.ItemId;
        var lastUploadTime = data.LastUploadTimeUnixMilliseconds;
        var uploadSource = data.UploadSource;
        var listings = data.Listings;
        var sales = data.Sales;

        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        var lastUpdated = await EnsureLastUpdated(worldId, itemId);
        
        // Create a transaction to update all of the data atomically
        var trans = db.CreateTransaction();
        trans.AddCondition(Condition.StringEqual(lastUpdatedKey, lastUpdated));
        
        // Queue an update to the listings
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        var existingListingIdsRaw = await db.StringGetAsync(listingsKey);
        var existingListingIds = ParseObjectIds(existingListingIdsRaw);
        SetListingsAtomic(trans, worldId, itemId, existingListingIds, listings);
        
        // Queue an update to the sales
        var salesKey = GetSalesIndexKey(worldId, itemId);
        var existingSaleIdsRaw = await db.StringGetAsync(salesKey);
        var existingSaleIds = ParseObjectIds(existingSaleIdsRaw);
        SetSalesAtomic(trans, worldId, itemId, existingSaleIds, sales);
        
        // Queue an update to the upload source
        SetSourceAtomic(trans, worldId, itemId, uploadSource);

        // Queue an update to the last updated time
        SetLastUpdatedAtomic(trans, worldId, itemId, lastUploadTime);
        
        // Execute the transaction. If this fails, we'll just assume that newer data
        // was written first and move on.
        await trans.ExecuteAsync();
    }
    
    private async Task<long> EnsureLastUpdated(uint worldId, uint itemId)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        await db.StringSetAsync(lastUpdatedKey, 0, when: When.NotExists);
        return (long)await db.StringGetAsync(lastUpdatedKey);
    }
    
    private static void SetLastUpdatedAtomic(IDatabaseAsync trans, uint worldId, uint itemId, long timestamp)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        _ = trans.StringSetAsync(lastUpdatedKey, timestamp);
    }
    
    private static async Task<string> GetSource(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        var source = await db.StringGetAsync(sourceKey);
        return source;
    }
    
    private static void SetSourceAtomic(IDatabaseAsync trans, uint worldId, uint itemId, string source)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        _ = trans.StringSetAsync(sourceKey, source);
    }

    private static async Task<List<ListingSimple>> GetListings(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        
        var listingIdsRaw = await db.StringGetAsync(listingsKey);
        if (listingIdsRaw.IsNullOrEmpty)
        {
            return new List<ListingSimple>();
        }

        var listingIds = ParseObjectIds(listingIdsRaw);
        return await listingIds.ToAsyncEnumerable()
            .SelectAwait(async id =>
            {
                var listingKey = GetListingKey(worldId, itemId, id);
                return new ListingSimple
                {
                    ListingId = await db.HashGetAsync(listingKey, "id"),
                    Hq = (bool)await db.HashGetAsync(listingKey, "hq"),
                    OnMannequin = (bool)await db.HashGetAsync(listingKey, "mann"),
                    PricePerUnit = (uint)await db.HashGetAsync(listingKey, "ppu"),
                    Quantity = (uint)await db.HashGetAsync(listingKey, "q"),
                    DyeId = (uint)await db.HashGetAsync(listingKey, "did"),
                    CreatorId = await db.HashGetAsync(listingKey, "cid"),
                    CreatorName = await db.HashGetAsync(listingKey, "cname"),
                    LastReviewTimeUnixSeconds = (long)await db.HashGetAsync(listingKey, "t"),
                    RetainerId = await db.HashGetAsync(listingKey, "rid"),
                    RetainerName = await db.HashGetAsync(listingKey, "rname"),
                    RetainerCityId = (int)await db.HashGetAsync(listingKey, "rcid"),
                    SellerId = (string)await db.HashGetAsync(listingKey, "sid"),
                };
            })
            .ToListAsync();
    }
    
    private static void SetListingsAtomic(IDatabaseAsync trans, uint worldId, uint itemId, IEnumerable<Guid> existingListingIds, IList<ListingSimple> listings)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        
        // Delete the existing listings
        foreach (var listingId in existingListingIds)
        {
            var listingKey = GetListingKey(worldId, itemId, listingId);
            _ = trans.KeyDeleteAsync(listingKey);
        }
        
        // Set the updated listings
        var newListingIds = listings.Select(_ => Guid.NewGuid()).ToList();
        foreach (var (listing, listingId) in listings.Zip(newListingIds))
        {
            var listingKey = GetListingKey(worldId, itemId, listingId);
            _ = trans.HashSetAsync(listingKey, new []
            {
                new HashEntry("id", listing.ListingId ?? ""),
                new HashEntry("hq", listing.Hq),
                new HashEntry("mann", listing.OnMannequin),
                new HashEntry("ppu", listing.PricePerUnit),
                new HashEntry("q", listing.Quantity),
                new HashEntry("did", listing.DyeId),
                new HashEntry("cid", listing.CreatorId ?? ""),
                new HashEntry("cname", listing.CreatorName ?? ""),
                new HashEntry("t", listing.LastReviewTimeUnixSeconds),
                new HashEntry("rid", listing.RetainerId ?? ""),
                new HashEntry("rname", listing.RetainerName ?? ""),
                new HashEntry("rcid", listing.RetainerCityId),
                new HashEntry("sid", listing.SellerId ?? ""),
            });
        }
        
        // Update the listings index
        _ = trans.StringSetAsync(listingsKey, string.Join(':', newListingIds.Select(id => id.ToString())));
    }
    
    private static async Task<List<SaleSimple>> GetSales(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var salesKey = GetSalesIndexKey(worldId, itemId);
        
        var saleIdsRaw = await db.StringGetAsync(salesKey);
        if (saleIdsRaw.IsNullOrEmpty)
        {
            return new List<SaleSimple>();
        }

        var saleIds = ParseObjectIds(saleIdsRaw);
        return await saleIds.ToAsyncEnumerable()
            .SelectAwait(async id =>
            {
                var listingKey = GetListingKey(worldId, itemId, id);
                return new SaleSimple
                {
                    Hq = (bool)await db.HashGetAsync(listingKey, "hq"),
                    PricePerUnit = (uint)await db.HashGetAsync(listingKey, "ppu"),
                    Quantity = (uint)await db.HashGetAsync(listingKey, "q"),
                    BuyerName = await db.HashGetAsync(listingKey, "bn"),
                    TimestampUnixSeconds = (long)await db.HashGetAsync(listingKey, "t"),
                };
            })
            .ToListAsync();
    }
    
    private static void SetSalesAtomic(IDatabaseAsync trans, uint worldId, uint itemId, IEnumerable<Guid> existingSaleIds, IList<SaleSimple> sales)
    {
        var salesKey = GetSalesIndexKey(worldId, itemId);
        
        // Delete the existing sales
        foreach (var saleId in existingSaleIds)
        {
            var saleKey = GetSaleKey(worldId, itemId, saleId);
            _ = trans.KeyDeleteAsync(saleKey);
        }
        
        // Set the updated sales
        var newSaleIds = sales.Select(_ => Guid.NewGuid()).ToList();
        foreach (var (sale, saleId) in sales.Zip(newSaleIds))
        {
            var saleKey = GetSaleKey(worldId, itemId, saleId);
            _ = trans.HashSetAsync(saleKey, new []
            {
                new HashEntry("hq", sale.Hq),
                new HashEntry("ppu", sale.PricePerUnit),
                new HashEntry("q", sale.Quantity),
                new HashEntry("bn", sale.BuyerName),
                new HashEntry("t", sale.TimestampUnixSeconds),
            });
        }
        
        // Update the sales index
        _ = trans.StringSetAsync(salesKey, string.Join(':', newSaleIds.Select(id => id.ToString())));
    }

    private static IEnumerable<Guid> ParseObjectIds(RedisValue v)
    {
        if (v.IsNull)
        {
            return Enumerable.Empty<Guid>();
        }

        var vStr = (string)v;
        return vStr.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse);
    }
    
    private static string GetUploadSourceKey(uint worldId, uint itemId)
    {
        return $"{worldId}:{itemId}:Source";
    }

    private static string GetLastUpdatedKey(uint worldId, uint itemId)
    {
        return $"{worldId}:{itemId}:LastUpdated";
    }
    
    private static string GetListingsIndexKey(uint worldId, uint itemId)
    {
        return $"{worldId}:{itemId}:Listings";
    }
    
    private static string GetListingKey(uint worldId, uint itemId, Guid listingId)
    {
        return $"{worldId}:{itemId}:Listings:{listingId.ToString()}";
    }
    
    private static string GetSalesIndexKey(uint worldId, uint itemId)
    {
        return $"{worldId}:{itemId}:Sales";
    }
    
    private static string GetSaleKey(uint worldId, uint itemId, Guid saleId)
    {
        return $"{worldId}:{itemId}:Sales:{saleId.ToString()}";
    }
}