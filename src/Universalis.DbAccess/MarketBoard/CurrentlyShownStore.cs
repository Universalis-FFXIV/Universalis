using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.Entities;
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
        IEnumerable<ListingSimple> listings;
        IEnumerable<SaleSimple> sales;
        do
        {
            lastUpdated = await EnsureLastUpdated(worldId, itemId);
            
            var trans = db.CreateTransaction();
            trans.AddCondition(Condition.StringEqual(lastUpdatedKey, lastUpdated));

            var sourceTask = GetSource(db, worldId, itemId);
            var listingsTask = GetListings(db, worldId, itemId);
            var salesTask = GetSales(db, worldId, itemId);
            await Task.WhenAll(sourceTask, listingsTask, salesTask);
            
            source = await sourceTask;
            listings = await listingsTask;
            sales = await salesTask;

            transactionExecuted = await trans.ExecuteAsync();
        } while (!transactionExecuted);
        
        return new CurrentlyShownSimple(worldId, itemId, lastUpdated, source, listings.ToList(), sales.ToList());
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

    private static async Task<ListingSimple[]> GetListings(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        
        var listingIdsRaw = await db.StringGetAsync(listingsKey);
        if (listingIdsRaw.IsNullOrEmpty)
        {
            return Array.Empty<ListingSimple>();
        }

        var listingIds = ParseObjectIds(listingIdsRaw);
        var listingFetches = listingIds
            .Select(async id =>
            {
                var listingKey = GetListingKey(worldId, itemId, id);
                var listingEntries = await db.HashGetAllAsync(listingKey);
                var listing = listingEntries.ToDictionary();
                return new ListingSimple
                {
                    ListingId = GetValueString(listing, "id"),
                    Hq = GetValueBool(listing, "hq"),
                    OnMannequin = GetValueBool(listing, "mann"),
                    PricePerUnit = GetValueUInt32(listing, "ppu"),
                    Quantity = GetValueUInt32(listing, "q"),
                    DyeId = GetValueUInt32(listing, "did"),
                    CreatorId = GetValueString(listing, "cid"),
                    CreatorName = GetValueString(listing, "cname"),
                    LastReviewTimeUnixSeconds = GetValueInt64(listing, "t"),
                    RetainerId = GetValueString(listing, "rid"),
                    RetainerName = GetValueString(listing, "rname"),
                    RetainerCityId = GetValueInt32(listing, "rcid"),
                    SellerId = GetValueString(listing, "sid"),
                    Materia = GetValueMateriaArray(listing, "mat"),
                };
            });
        
        return await Task.WhenAll(listingFetches);
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
                new HashEntry("mat", SerializeMateriaArray(listing.Materia)),
            });
        }
        
        // Update the listings index
        _ = trans.StringSetAsync(listingsKey, SerializeObjectIds(newListingIds));
    }
    
    private static async Task<SaleSimple[]> GetSales(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var salesKey = GetSalesIndexKey(worldId, itemId);
        
        var saleIdsRaw = await db.StringGetAsync(salesKey);
        if (saleIdsRaw.IsNullOrEmpty)
        {
            return Array.Empty<SaleSimple>();
        }

        var saleIds = ParseObjectIds(saleIdsRaw);
        var saleFetches = saleIds
            .Select(async id =>
            {
                var saleKey = GetSaleKey(worldId, itemId, id);
                var saleEntries = await db.HashGetAllAsync(saleKey);
                var sale = saleEntries.ToDictionary();
                return new SaleSimple
                {
                    Hq = GetValueBool(sale, "hq"),
                    PricePerUnit = GetValueUInt32(sale, "ppu"),
                    Quantity = GetValueUInt32(sale, "q"),
                    BuyerName = GetValueString(sale, "bn"),
                    TimestampUnixSeconds = GetValueInt64(sale, "t"),
                };
            });
        
        return await Task.WhenAll(saleFetches);
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
                new HashEntry("bn", sale.BuyerName ?? ""),
                new HashEntry("t", sale.TimestampUnixSeconds),
            });
        }
        
        // Update the sales index
        _ = trans.StringSetAsync(salesKey, SerializeObjectIds(newSaleIds));
    }
    
    private static uint GetValueUInt32(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? (uint)hash[key] : 0;
    }
    
    private static int GetValueInt32(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? (int)hash[key] : 0;
    }
    
    private static long GetValueInt64(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? (long)hash[key] : 0;
    }

    private static bool GetValueBool(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) && (bool)hash[key];
    }
    
    private static string GetValueString(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? hash[key] : "";
    }
    
    private static List<Materia> GetValueMateriaArray(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? ParseMateria(hash[key]).ToList() : new List<Materia>();
    }

    private static IEnumerable<Materia> ParseMateria(RedisValue v)
    {
        if (v.IsNull)
        {
            return Enumerable.Empty<Materia>();
        }

        var vStr = (string)v;
        return vStr.Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Select(m =>
            {
                var data = m.Split('-').Select(uint.Parse).ToArray();
                return new Materia
                {
                    MateriaId = data[0],
                    SlotId = data[1],
                };
            })
            .ToList();
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

    private static string SerializeMateriaArray(IEnumerable<Materia> materia)
    {
        return string.Join(':', materia.Select(m => $"{m.MateriaId}-{m.SlotId}"));
    }
    
    private static string SerializeObjectIds(IEnumerable<Guid> ids)
    {
        return string.Join(':', ids.Select(id => id.ToString()));
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