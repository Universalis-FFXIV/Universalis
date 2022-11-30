using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly IDatabase _db;

    public CurrentlyShownStore(IPersistentRedisMultiplexer redis)
    {
        _db = redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
    }

    public async Task<CurrentlyShown> GetData(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        var result = await FetchData(_db, worldId, itemId, cancellationToken);
        if (result == null)
        {
            return new CurrentlyShown();
        }

        return result;
    }

    public async Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        await StoreData(_db, data);
    }

    private static async Task StoreData(IDatabase db, CurrentlyShown data, TimeSpan? expiry = null)
    {
        var worldId = data.WorldId;
        var itemId = data.ItemId;
        var lastUploadTime = data.LastUploadTimeUnixMilliseconds;
        var uploadSource = data.UploadSource;
        var listings = data.Listings;

        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        var lastUpdated = await EnsureLastUpdated(db, worldId, itemId);

        // Create a transaction to update all of the data atomically
        var trans = db.CreateTransaction();
        trans.AddCondition(Condition.StringEqual(lastUpdatedKey, lastUpdated));

        // Queue an update to the listings
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        var existingListingIdsRaw = await db.StringGetAsync(listingsKey);
        var existingListingIds = ParseObjectIds(existingListingIdsRaw);
        SetListingsAtomic(trans, worldId, itemId, existingListingIds, listings, expiry);

        // Queue an update to the upload source
        SetSourceAtomic(trans, worldId, itemId, uploadSource, expiry);

        // Queue an update to the last updated time
        SetLastUpdatedAtomic(trans, worldId, itemId, lastUploadTime, expiry);

        // Execute the transaction. If this fails, we'll just assume that newer data
        // was written first and move on.
        await trans.ExecuteAsync();
    }
    
    private static async Task<RedisValue> EnsureLastUpdated(IDatabase db, uint worldId, uint itemId)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        await db.StringSetAsync(lastUpdatedKey, 0, when: When.NotExists);
        return await db.StringGetAsync(lastUpdatedKey);
    }
    
    private static void SetLastUpdatedAtomic(ITransaction trans, uint worldId, uint itemId, long timestamp, TimeSpan? expiry = null)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        _ = trans.StringSetAsync(lastUpdatedKey, timestamp, expiry);
    }
    
    private static async Task<string> GetSource(IDatabaseAsync db, uint worldId, uint itemId)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        var source = await db.StringGetAsync(sourceKey, flags: CommandFlags.PreferReplica);
        return source;
    }
    
    private static void SetSourceAtomic(ITransaction trans, uint worldId, uint itemId, string source, TimeSpan? expiry = null)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        _ = trans.StringSetAsync(sourceKey, source, expiry);
    }

    private static async Task<CurrentlyShown> FetchData(IDatabase db, uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        var lastUpdated = await EnsureLastUpdated(db, worldId, itemId);
        if (lastUpdated.IsNullOrEmpty || (long)lastUpdated == 0)
        {
            return null;
        }

        var sourceTask = GetSource(db, worldId, itemId);
        var listingsTask = GetListings(db, worldId, itemId, cancellationToken);
        await Task.WhenAll(sourceTask, listingsTask);

        var source = await sourceTask;
        var listings = await listingsTask;

        return new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = (long)lastUpdated,
            UploadSource = source,
            Listings = listings,
        };
    }

    private static async Task<List<Listing>> GetListings(IDatabaseAsync db, uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        
        var listingIdsRaw = await db.StringGetAsync(listingsKey, flags: CommandFlags.PreferReplica);
        if (listingIdsRaw.IsNullOrEmpty)
        {
            return new List<Listing>(0);
        }

        var listingIds = ParseObjectIds(listingIdsRaw);
        return await listingIds
            .ToAsyncEnumerable()
            .SelectAwait(async id =>
            {
                var listingKey = GetListingKey(worldId, itemId, id);
                var listingEntries = await db.HashGetAllAsync(listingKey, flags: CommandFlags.PreferReplica);
                var listing = listingEntries.ToDictionary();
                return new Listing
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
            })
            .ToListAsync(cancellationToken);
    }
    
    private static void SetListingsAtomic(ITransaction trans, uint worldId, uint itemId, IEnumerable<Guid> existingListingIds, IList<Listing> listings, TimeSpan? expiry = null)
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
            _ = trans.KeyExpireAsync(listingKey, expiry);
        }
        
        // Update the listings index
        _ = trans.StringSetAsync(listingsKey, SerializeObjectIds(newListingIds), expiry);
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
        return vStr!.Split(':', StringSplitOptions.RemoveEmptyEntries)
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
        if (v.IsNullOrEmpty)
        {
            return Enumerable.Empty<Guid>();
        }

        var vStr = (string)v;
        return vStr!.Split(':', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse);
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
        return $"{worldId}:{itemId}:Listings:{listingId}";
    }
}