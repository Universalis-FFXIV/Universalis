using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class CurrentlyShownStore : ICurrentlyShownStore
{
    private readonly IPersistentRedisMultiplexer _redis;
    private readonly IListingStore _listingStore;
    private readonly ILogger<CurrentlyShownStore> _logger;

    public CurrentlyShownStore(IPersistentRedisMultiplexer redis,
        IListingStore listingStore, ILogger<CurrentlyShownStore> logger)
    {
        _redis = redis;
        _listingStore = listingStore;
        _logger = logger;
    }

    public async Task<CurrentlyShown> GetData(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        var result = await FetchData(db, worldId, itemId, cancellationToken);
        return result ?? new CurrentlyShown();
    }

    public async Task SetData(CurrentlyShown data, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase(RedisDatabases.Instance0.CurrentData);
        await StoreData(db, data, null, cancellationToken);
    }

    private async Task StoreData(IDatabaseAsync db, CurrentlyShown data, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        var worldId = data.WorldId;
        var itemId = data.ItemId;
        var lastUploadTime = data.LastUploadTimeUnixMilliseconds;
        var uploadSource = data.UploadSource;
        var listings = data.Listings;

        await _listingStore.UpsertLive(listings, cancellationToken);
        await SetSource(db, worldId, itemId, uploadSource, expiry);
        await SetLastUpdated(db, worldId, itemId, lastUploadTime, expiry);
    }

    private static async Task<RedisValue> EnsureLastUpdated(IDatabaseAsync db, int worldId, int itemId)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        await db.StringSetAsync(lastUpdatedKey, 0, when: When.NotExists);
        return await db.StringGetAsync(lastUpdatedKey);
    }

    private static Task SetLastUpdated(IDatabaseAsync db, int worldId, int itemId, long timestamp,
        TimeSpan? expiry = null)
    {
        var lastUpdatedKey = GetLastUpdatedKey(worldId, itemId);
        return db.StringSetAsync(lastUpdatedKey, timestamp, expiry);
    }

    private static async Task<RedisValue> GetSource(IDatabaseAsync db, int worldId, int itemId)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        var source = await db.StringGetAsync(sourceKey, flags: CommandFlags.PreferReplica);
        return source;
    }

    private static Task SetSource(IDatabaseAsync db, int worldId, int itemId, string source, TimeSpan? expiry = null)
    {
        var sourceKey = GetUploadSourceKey(worldId, itemId);
        return db.StringSetAsync(sourceKey, source, expiry);
    }

    private async Task<CurrentlyShown> FetchData(IDatabaseAsync db, int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        var lastUpdated = await EnsureLastUpdated(db, worldId, itemId);
        if (lastUpdated.IsNullOrEmpty || (long)lastUpdated == 0)
        {
            return null;
        }

        var source = await GetSource(db, worldId, itemId);

        // Attempt to retrieve listings from Postgres
        var listings = new List<Listing>();
        try
        {
            var listingsEnumerable = await _listingStore.RetrieveLive(
                new ListingQuery { ItemId = itemId, WorldId = worldId },
                cancellationToken);
            listings = listingsEnumerable.ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings from primary database (item={ItemId}, world={WorldId})",
                itemId, worldId);
        }

        if (!listings.Any())
        {
            // Attempt to retrieve listings from the Redis primary
            try
            {
                listings = (await GetListings(db, worldId, itemId, cancellationToken))
                    .Select(l =>
                    {
                        // These are implicit in the index key, so they need to
                        // be added back separately.
                        l.ItemId = itemId;
                        l.WorldId = worldId;

                        if (string.IsNullOrEmpty(l.ListingId))
                        {
                            // Listing IDs from some uploaders are empty; this needs to be fixed
                            // but this should be a decent workaround.
                            l.ListingId = $"dirty:{Guid.NewGuid()}";
                        }

                        return l;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to retrieve listings from secondary database (item={ItemId}, world={WorldId})", itemId,
                    worldId);
            }

            // Re-save the listings in Postgres
            _ = SaveListings(listings, itemId, worldId);
        }

        return new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = (long)lastUpdated,
            UploadSource = source,
            Listings = listings,
        };
    }

    private async Task SaveListings(IEnumerable<Listing> listings, int itemId, int worldId)
    {
        try
        {
            await _listingStore.UpsertLive(listings);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Failed to store listings from secondary database in primary database (item={ItemId}, world={WorldId})",
                itemId, worldId);
        }
    }

    private async Task<string> GetListingIds(IDatabaseAsync db, int worldId, int itemId)
    {
        var listingsKey = GetListingsIndexKey(worldId, itemId);
        var listingIds = await db.StringGetAsync(listingsKey, flags: CommandFlags.PreferReplica);
        return listingIds;
    }

    private async Task<List<Listing>> GetListings(IDatabaseAsync db, int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        var listingIdsRaw = await GetListingIds(db, worldId, itemId);
        if (string.IsNullOrEmpty(listingIdsRaw))
        {
            return new List<Listing>(0);
        }

        var listingKeys = new RedisValue[] { "id", "hq", "mann", "ppu", "q", "did", "cid", "cname", "t", "rid", "rname", "rcid", "sid", "mat" };
        var opts = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 4,
            CancellationToken = cancellationToken,
        };

        var transformBlock = new TransformBlock<Guid, Listing>(async id =>
        {
            var listingKey = GetListingKey(worldId, itemId, id);
            var listingEntries = await db.HashGetAsync(listingKey, listingKeys, flags: CommandFlags.PreferReplica);

            return new Listing
            {
                ListingId = (string)listingEntries[0] ?? "",
                Hq = (bool)listingEntries[1],
                OnMannequin = (bool)listingEntries[2],
                PricePerUnit = (int)listingEntries[3],
                Quantity = (int)listingEntries[4],
                DyeId = (int)listingEntries[5],
                CreatorId = (string)listingEntries[6] ?? "",
                CreatorName = (string)listingEntries[7] ?? "",
                LastReviewTime = DateTimeOffset.FromUnixTimeSeconds((long)listingEntries[8]).UtcDateTime,
                RetainerId = (string)listingEntries[9] ?? "",
                RetainerName = (string)listingEntries[10] ?? "",
                RetainerCityId = (int)listingEntries[11],
                SellerId = (string)listingEntries[12] ?? "",
                Materia = GetValueMateriaArray(listingEntries[13]),
            };
        }, opts);

        foreach (var id in ParseObjectIds(listingIdsRaw))
        {
            transformBlock.Post(id);
        }
        transformBlock.Complete();

        return await transformBlock
            .ReceiveAllAsync(cancellationToken)
            .ToListAsync(cancellationToken);
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