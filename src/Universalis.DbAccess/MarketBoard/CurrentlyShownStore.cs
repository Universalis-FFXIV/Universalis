using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly ICacheRedisMultiplexer _cache;
    private readonly IPersistentRedisMultiplexer _redis;
    private readonly ILogger<CurrentlyShownStore> _logger;

    public CurrentlyShownStore(ICacheRedisMultiplexer cache, IPersistentRedisMultiplexer redis, ILogger<CurrentlyShownStore> logger)
    {
        _cache = cache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<CurrentlyShown> GetData(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        var cache = _redis.GetDatabase(RedisDatabases.Cache.Listings);
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        var result = await FetchData(db, cache, worldId, itemId, cancellationToken);
        if (result == null)
        {
            return new CurrentlyShown();
        }

        return result;
    }

    public async Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        var cache = _redis.GetDatabase(RedisDatabases.Cache.Listings);
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        await StoreData(db, cache, data);
    }

    private static async Task StoreData(IDatabase db, IDatabase cache, CurrentlyShown data, TimeSpan? expiry = null)
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

        // Purge the cache
        await cache.KeyDeleteAsync(GetUploadSourceKey(worldId, itemId));
        await cache.KeyDeleteAsync(GetListingsIndexKey(worldId, itemId));
    }

    private static async Task<RedisValue> EnsureLastUpdated(IDatabase db, int worldId, int itemId)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        await db.StringSetAsync(lastUpdatedKey, 0, when: When.NotExists);
        return await db.StringGetAsync(lastUpdatedKey);
    }

    private static void SetLastUpdatedAtomic(ITransaction trans, int worldId, int itemId, long timestamp, TimeSpan? expiry = null)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        _ = trans.StringSetAsync(lastUpdatedKey, timestamp, expiry);
    }

    private static async Task<RedisValue> GetSource(IDatabaseAsync db, int worldId, int itemId)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        var source = await db.StringGetAsync(sourceKey, flags: CommandFlags.PreferReplica);
        return source;
    }

    private static async Task SetSource(IDatabaseAsync db, int worldId, int itemId, string source, TimeSpan? expiry = null)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        await db.StringSetAsync(sourceKey, source, expiry);
    }

    private static void SetSourceAtomic(ITransaction trans, int worldId, int itemId, string source, TimeSpan? expiry = null)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        _ = trans.StringSetAsync(sourceKey, source, expiry);
    }

    private async Task<CurrentlyShown> FetchData(IDatabase db, IDatabase cache, int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        var lastUpdated = await EnsureLastUpdated(db, worldId, itemId);
        if (lastUpdated.IsNullOrEmpty || (long)lastUpdated == 0)
        {
            return null;
        }

        var source = await GetSourceWithCache(db, cache, worldId, itemId);
        var listings = await GetListings(db, cache, worldId, itemId, cancellationToken);

        return new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = (long)lastUpdated,
            UploadSource = source,
            Listings = listings,
        };
    }

    private async Task<string> GetSourceWithCache(IDatabase db, IDatabase cache, int worldId, int itemId)
    {
        // Try to retrieve the source from the cache
        try
        {
            var cached = await GetSource(cache, worldId, itemId);
            if (!cached.IsNullOrEmpty)
            {
                return cached;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve upload source from cache (item={ItemId}, world={WorldId})", itemId, worldId);
        }

        // Fetch the source from the database
        var source = await GetSource(db, worldId, itemId);

        // Store the result in the cache
        try
        {
            await SetSource(cache, worldId, itemId, source, TimeSpan.FromHours(1));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store upload source in the cache (item={ItemId}, world={WorldId})", itemId, worldId);
        }

        return source;
    }

    private async Task<string> GetListingIdsWithCache(IDatabaseAsync db, IDatabaseAsync cache, int worldId, int itemId)
    {
        // Try to retrieve the listing IDs from the cache
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        try
        {
            var cached = await cache.StringGetAsync(listingsKey, flags: CommandFlags.PreferReplica);
            if (!cached.IsNullOrEmpty)
            {
                return cached;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listing IDs from cache (item={ItemId}, world={WorldId})", itemId, worldId);
        }

        // Fetch the listing IDs from the database
        var listingIds = await db.StringGetAsync(listingsKey, flags: CommandFlags.PreferReplica);

        // Store the result in the cache
        try
        {
            await cache.StringSetAsync(listingsKey, listingIds, expiry: TimeSpan.FromHours(1));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store listing IDs in the cache (item={ItemId}, world={WorldId})", itemId, worldId);
        }

        return listingIds;
    }

    private async Task<List<Listing>> GetListings(IDatabaseAsync db, IDatabaseAsync cache, int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);

        var listingIdsRaw = await GetListingIdsWithCache(db, cache, worldId, itemId);
        if (string.IsNullOrEmpty(listingIdsRaw))
        {
            return new List<Listing>(0);
        }

        var listingKeys = new RedisValue[] { "id", "hq", "mann", "ppu", "q", "did", "cid", "cname", "t", "rid", "rname", "rcid", "sid", "mat" };
        var listingIds = ParseObjectIds(listingIdsRaw);
        return await listingIds
            .ToAsyncEnumerable()
            .SelectAwait(async id =>
            {
                
                var listingKey = GetListingKey(worldId, itemId, id);
                var listingEntries = await db.HashGetAsync(listingKey, listingKeys, flags: CommandFlags.PreferReplica);

                return new Listing
                {
                    ListingId = GetValueString(listingEntries[0]),
                    Hq = GetValueBool(listingEntries[1]),
                    OnMannequin = GetValueBool(listingEntries[2]),
                    PricePerUnit = GetValueInt32(listingEntries[3]),
                    Quantity = GetValueInt32(listingEntries[4]),
                    DyeId = GetValueInt32(listingEntries[5]),
                    CreatorId = GetValueString(listingEntries[6]),
                    CreatorName = GetValueString(listingEntries[7]),
                    LastReviewTimeUnixSeconds = GetValueInt64(listingEntries[8]),
                    RetainerId = GetValueString(listingEntries[9]),
                    RetainerName = GetValueString(listingEntries[10]),
                    RetainerCityId = GetValueInt32(listingEntries[11]),
                    SellerId = GetValueString(listingEntries[12]),
                    Materia = GetValueMateriaArray(listingEntries[13]),
                };
            })
            .ToListAsync(cancellationToken);
    }

    private static void SetListingsAtomic(ITransaction trans, int worldId, int itemId, IEnumerable<Guid> existingListingIds, IList<Listing> listings, TimeSpan? expiry = null)
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
            _ = trans.HashSetAsync(listingKey, new[]
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

    private static int GetValueInt32(RedisValue value)
    {
        return (int)value;
    }

    private static long GetValueInt64(RedisValue value)
    {
        return (long)value;
    }

    private static bool GetValueBool(RedisValue value)
    {
        return (bool)value;
    }

    private static string GetValueString(RedisValue value)
    {
        return value.IsNull ? "" : value;
    }

    private static List<Materia> GetValueMateriaArray(RedisValue value)
    {
        return (value.IsNullOrEmpty ? Enumerable.Empty<Materia>() : ParseMateria(value)).ToList();
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
                var data = m.Split('-').Select(int.Parse).ToArray();
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

    private static string GetUploadSourceKey(int worldId, int itemId)
    {
        return $"{worldId}:{itemId}:Source";
    }

    private static string GetLastUpdatedKey(int worldId, int itemId)
    {
        return $"{worldId}:{itemId}:LastUpdated";
    }

    private static string GetListingsIndexKey(int worldId, int itemId)
    {
        return $"{worldId}:{itemId}:Listings";
    }

    private static string GetListingKey(int worldId, int itemId, Guid listingId)
    {
        return $"{worldId}:{itemId}:Listings:{listingId}";
    }
}