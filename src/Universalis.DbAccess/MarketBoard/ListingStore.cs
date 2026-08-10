using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyCaching.Core;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Prometheus;
using StackExchange.Redis;
using Universalis.Common.GameData;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public class ListingStore : IListingStore
{
    private static readonly Counter LocalCacheHits =
        Prometheus.Metrics.CreateCounter("universalis_listing_local_cache_hit", "");

    private static readonly Counter
        LocalCacheMisses = Prometheus.Metrics.CreateCounter("universalis_listing_local_cache_miss", "");

    private static readonly Counter LocalCacheUpdates =
        Prometheus.Metrics.CreateCounter("universalis_listing_local_cache_update", "");

    private static readonly Histogram CreateCommandDuration =
        Prometheus.Metrics.CreateHistogram("universalis_listing_create_command", "");

    private static readonly Histogram ExecuteReaderDuration =
        Prometheus.Metrics.CreateHistogram("universalis_listing_execute_reader", "", new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(1, 2, 16),
        });

    private static readonly Histogram RowsReadCount =
        Prometheus.Metrics.CreateHistogram("universalis_listing_rows_read", "", new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(1, 2, 16),
        });

    private static readonly TimeSpan LocalListingsCacheTime = TimeSpan.FromMinutes(5);

    private readonly ILogger<ListingStore> _logger;
    private readonly IEasyCachingProvider _easyCachingProvider;
    private readonly NpgsqlDataSource _dataSource;
    private readonly IPersistentRedisMultiplexer _cache;
    private readonly IWorldToDcRegion _worldToDcRegion;

    public ListingStore(NpgsqlDataSource dataSource, IEasyCachingProvider easyCachingProvider,
        ILogger<ListingStore> logger, IPersistentRedisMultiplexer cache, IWorldToDcRegion worldToDcRegion)
    {
        _easyCachingProvider = easyCachingProvider;
        _dataSource = dataSource;
        _logger = logger;
        _cache = cache;
        _worldToDcRegion = worldToDcRegion;
    }

    public async Task<IList<Listing>> DeleteLive(ListingQuery query, string retainedRetainerId = null, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.DeleteLive");
        var hasRetention = !string.IsNullOrEmpty(retainedRetainerId);
        var commandText = hasRetention
            ? $"DELETE FROM listing WHERE item_id = $1 AND world_id = $2 AND retainer_id <> $3 RETURNING {ListingColumns}"
            : $"DELETE FROM listing WHERE item_id = $1 AND world_id = $2 RETURNING {ListingColumns}";

        var uploadedAt = DateTimeOffset.Now;
        List<Listing> removed;

        if (!hasRetention)
        {
            await using var command = _dataSource.CreateCommand(commandText);
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                removed = await ReadListings(reader, cancellationToken);
                activity?.AddTag("rowsUpdated", removed.Count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete listings (world={}, item={})", query.WorldId,
                    query.ItemId);
                throw;
            }
            await _easyCachingProvider.RemoveAsync(ListingsKey(query.WorldId, query.ItemId), cancellationToken);
            await WriteMinListingCache(query.WorldId, query.ItemId, new List<Listing>(), uploadedAt);
            return removed;
        }

        // Retention path: the scoped DELETE preserves the retainer's rows, so the
        // min-listing aggregates must reflect the surviving data, not an empty set.
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using (var command = new NpgsqlCommand(commandText, connection))
        {
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
            command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });
            command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = retainedRetainerId });
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                removed = await ReadListings(reader, cancellationToken);
                activity?.AddTag("rowsUpdated", removed.Count);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete listings (world={}, item={})", query.WorldId,
                    query.ItemId);
                throw;
            }
        }
        await _easyCachingProvider.RemoveAsync(ListingsKey(query.WorldId, query.ItemId), cancellationToken);
        var effectiveListings = await ReadMinListings(connection, query.WorldId, query.ItemId, cancellationToken);
        await WriteMinListingCache(query.WorldId, query.ItemId, effectiveListings, uploadedAt);
        return removed;
    }

    public async Task<IList<Listing>> ReplaceLive(ICollection<Listing> listings, string retainedRetainerId = null, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.ReplaceLive");
        var rowsUpdated = 0;
        var replaced = new List<Listing>();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Get the current timestamp for the batch
        var uploadedAt = DateTimeOffset.Now;

        // Listings are grouped for better exceptions if a batch fails; exceptions can be
        // filtered by world and item.
        var groupedListings = listings.GroupBy(l => new WorldItemPair(l.WorldId, l.ItemId));
        foreach (var listingGroup in groupedListings)
        {
            var (worldID, itemID) = listingGroup.Key;

            var listingIds = listingGroup.Select(l => l.ListingId).ToArray();
            var staleItemIds = await FindItemsLosingListings(connection, listingIds, worldID, itemID, cancellationToken);

            // Npgsql batches have an implicit transaction around them
            // https://www.npgsql.org/doc/basic-usage.html#batching
            await using var batch = new NpgsqlBatch(connection);
            batch.BatchCommands.Add(CreateDisplacingDelete(itemID, worldID, retainedRetainerId));

            foreach (var listing in listingGroup)
            {
                batch.BatchCommands.Add(CreateUpsert(listing, uploadedAt));
            }

            try
            {
                // The DELETE is the batch's first statement, so its RETURNING rows are
                // the reader's first result set. The inserts return no rows, so the
                // loop below reads nothing - but it is not optional: RecordsAffected
                // only accumulates over statements the reader has advanced past, so
                // without it rowsUpdated would report the DELETE alone (3 rather than
                // 5 for a 3-delete, 2-insert batch) instead of the whole batch, which
                // is what ExecuteNonQueryAsync reported here before.
                // The reader must also be consumed and closed before the connection
                // is reused below.
                await using (var reader = await batch.ExecuteReaderAsync(cancellationToken))
                {
                    replaced.AddRange(await ReadListings(reader, cancellationToken));
                    while (await reader.NextResultAsync(cancellationToken))
                    {
                        // Advancing is the point; the inserts return no rows to read.
                    }

                    rowsUpdated += reader.RecordsAffected;
                }

                await _easyCachingProvider.RemoveAsync(ListingsKey(worldID, itemID), cancellationToken);
                foreach (var staleItemId in staleItemIds)
                {
                    await _easyCachingProvider.RemoveAsync(ListingsKey(worldID, staleItemId), cancellationToken);
                }
            }
            catch (Exception e)
            {
                activity?.AddTag("rowsUpdated", rowsUpdated);
                _logger.LogError(e, "Failed to insert listings (world={}, item={})", worldID,
                    itemID);
                throw;
            }

            var effectiveListings = string.IsNullOrEmpty(retainedRetainerId)
                ? listings
                : await ReadMinListings(connection, worldID, itemID, cancellationToken);
            await WriteMinListingCache(worldID, itemID, effectiveListings, uploadedAt);
        }

        activity?.AddTag("rowsUpdated", rowsUpdated);
        return replaced;
    }

    /// <summary>
    /// Finds items other than <paramref name="itemId"/> that currently own one of
    /// <paramref name="listingIds"/> on this world, and so are about to lose it.
    /// </summary>
    /// <remarks>
    /// The batch's DELETE only clears the current (item, world), so any conflict on
    /// (listing_id, world_id) implies a row with the same key under a different
    /// item_id, which <c>DO UPDATE SET item_id = ...</c> moves to the current item.
    /// Without evicting the old item's cache key, <c>listing5:{world}:{oldItem}</c>
    /// would keep serving a listing that has moved away until its TTL expires.
    /// </remarks>
    private static async Task<HashSet<int>> FindItemsLosingListings(NpgsqlConnection connection,
        string[] listingIds, int worldId, int itemId, CancellationToken cancellationToken)
    {
        var staleItemIds = new HashSet<int>();
        await using var command = new NpgsqlCommand(
            "SELECT DISTINCT item_id FROM listing WHERE listing_id = ANY($1) AND world_id = $2 AND item_id <> $3",
            connection);
        command.Parameters.Add(new NpgsqlParameter<string[]> { TypedValue = listingIds });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = worldId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = itemId });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            staleItemIds.Add(reader.GetInt32(0));
        }

        return staleItemIds;
    }

    /// <summary>
    /// The DELETE that clears the board for an upload, returning the rows it removed.
    /// </summary>
    /// <remarks>
    /// RETURNING hands back exactly the rows this upload displaced, which is the
    /// pre-upload board for this (item, world). Callers that need to diff against it
    /// therefore need no separate read - and when a retainer is retained, the scoped
    /// DELETE leaves that retainer's rows in place, so they are absent from the
    /// displaced set and cannot be mistaken for removals.
    /// </remarks>
    private static NpgsqlBatchCommand CreateDisplacingDelete(int itemId, int worldId, string retainedRetainerId)
    {
        var retaining = !string.IsNullOrEmpty(retainedRetainerId);
        var command = new NpgsqlBatchCommand(retaining
            ? $"DELETE FROM listing WHERE item_id = $1 AND world_id = $2 AND retainer_id <> $3 RETURNING {ListingColumns}"
            : $"DELETE FROM listing WHERE item_id = $1 AND world_id = $2 RETURNING {ListingColumns}")
        {
            Parameters =
            {
                new NpgsqlParameter<int> { TypedValue = itemId },
                new NpgsqlParameter<int> { TypedValue = worldId },
            },
        };

        if (retaining)
        {
            command.Parameters.Add(new NpgsqlParameter<string> { TypedValue = retainedRetainerId });
        }

        return command;
    }

    /// <summary>
    /// Writes one uploaded listing, overwriting any row that already holds its key.
    /// </summary>
    /// <remarks>
    /// The batch's DELETE clears rows for the current (item, world) before these
    /// inserts, so a remaining conflict on the (listing_id, world_id) primary key
    /// implies a pre-existing row with the same key but a different item_id. Treat
    /// the new upload as authoritative and overwrite, rather than silently dropping
    /// it - see <see cref="FindItemsLosingListings"/> for the cache consequence.
    /// </remarks>
    private static NpgsqlBatchCommand CreateUpsert(Listing listing, DateTimeOffset uploadedAt)
    {
        return new NpgsqlBatchCommand(
            """
            INSERT INTO listing
            (listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id,
             creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, uploaded_at,
             source)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16)
            ON CONFLICT (listing_id, world_id) DO UPDATE SET
                item_id = EXCLUDED.item_id,
                hq = EXCLUDED.hq,
                on_mannequin = EXCLUDED.on_mannequin,
                materia = EXCLUDED.materia,
                unit_price = EXCLUDED.unit_price,
                quantity = EXCLUDED.quantity,
                dye_id = EXCLUDED.dye_id,
                creator_name = EXCLUDED.creator_name,
                last_review_time = EXCLUDED.last_review_time,
                retainer_id = EXCLUDED.retainer_id,
                retainer_name = EXCLUDED.retainer_name,
                retainer_city_id = EXCLUDED.retainer_city_id,
                uploaded_at = EXCLUDED.uploaded_at,
                source = EXCLUDED.source;
            """)
        {
            Parameters =
            {
                new NpgsqlParameter<string> { TypedValue = listing.ListingId },
                new NpgsqlParameter<int> { TypedValue = listing.ItemId },
                new NpgsqlParameter<int> { TypedValue = listing.WorldId },
                new NpgsqlParameter<bool> { TypedValue = listing.Hq },
                new NpgsqlParameter<bool> { TypedValue = listing.OnMannequin },
                ConvertMateriaToParameter(listing.Materia),
                new NpgsqlParameter<int> { TypedValue = listing.PricePerUnit },
                new NpgsqlParameter<int> { TypedValue = listing.Quantity },
                new NpgsqlParameter<int> { TypedValue = listing.DyeId },
                new NpgsqlParameter<string> { TypedValue = listing.CreatorName },
                new NpgsqlParameter<DateTime> { TypedValue = listing.LastReviewTime },
                new NpgsqlParameter<string> { TypedValue = listing.RetainerId },
                new NpgsqlParameter<string> { TypedValue = listing.RetainerName },
                new NpgsqlParameter<int> { TypedValue = listing.RetainerCityId },
                new NpgsqlParameter<DateTime> { TypedValue = uploadedAt.UtcDateTime },
                new NpgsqlParameter<string> { TypedValue = listing.Source },
            },
        };
    }

    /// <summary>
    /// The listing columns, in the order <see cref="ReadListing"/> expects them.
    /// Shared between SELECT and DELETE ... RETURNING so the two cannot drift.
    /// </summary>
    private const string ListingColumns =
        "listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, " +
        "creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, uploaded_at, source";

    private static async Task<List<Listing>> ReadListings(NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        var listings = new List<Listing>();
        while (await reader.ReadAsync(cancellationToken))
        {
            listings.Add(ReadListing(reader));
        }

        return listings;
    }

    /// <summary>
    /// Maps one row of <see cref="ListingColumns"/>. Columns are read in ordinal
    /// order so this stays valid under <see cref="CommandBehavior.SequentialAccess"/>.
    /// </summary>
    private static Listing ReadListing(NpgsqlDataReader reader)
    {
        return new Listing
        {
            ListingId = string.Intern(reader.GetString(0)),
            ItemId = reader.GetInt32(1),
            WorldId = reader.GetInt32(2),
            Hq = reader.GetBoolean(3),
            OnMannequin = reader.GetBoolean(4),
            Materia = ReadMateriaFromReader(reader),
            PricePerUnit = reader.GetInt32(6),
            Quantity = reader.GetInt32(7),
            DyeId = reader.GetInt32(8),
            CreatorId = null,
            CreatorName = reader.GetString(9),
            LastReviewTime = reader.GetDateTime(10),
            RetainerId = string.Intern(reader.GetString(11)),
            RetainerName = reader.GetString(12),
            RetainerCityId = reader.GetInt32(13),
            SellerId = null,
            UpdatedAt = reader.GetDateTime(14),
            Source = string.Intern(reader.GetString(15)),
        };
    }

    // Aggregate-only recompute of min prices after a retention write. Retained
    // rows survive the DELETE without round-tripping through the caller, so the
    // uploaded collection is not the full post-write set.
    private static async Task<List<Listing>> ReadMinListings(NpgsqlConnection connection, int worldID, int itemID,
        CancellationToken cancellationToken)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.ReadMinListings");

        await using var cmd = new NpgsqlCommand(
            "SELECT MIN(unit_price) FILTER (WHERE hq) AS min_hq, MIN(unit_price) FILTER (WHERE NOT hq) AS min_nq " +
            "FROM listing WHERE item_id = $1 AND world_id = $2",
            connection);
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = itemID });
        cmd.Parameters.Add(new NpgsqlParameter<int> { TypedValue = worldID });
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        int? minHq = null;
        int? minNq = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            if (!await reader.IsDBNullAsync(0, cancellationToken)) minHq = reader.GetInt32(0);
            if (!await reader.IsDBNullAsync(1, cancellationToken)) minNq = reader.GetInt32(1);
        }

        var result = new List<Listing>(2);
        if (minHq != null)
        {
            result.Add(new Listing { ListingId = "min-hq", Hq = true, PricePerUnit = minHq.Value, Materia = new List<Materia>() });
        }
        if (minNq != null)
        {
            result.Add(new Listing { ListingId = "min-nq", Hq = false, PricePerUnit = minNq.Value, Materia = new List<Materia>() });
        }
        return result;
    }

    private async Task WriteMinListingCache(int worldId, int itemId, ICollection<Listing> listings, DateTimeOffset uploadedAt)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.WriteMinListingCache");

        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var minListingNq = listings.Where(l => !l.Hq).MinBy(l => l.PricePerUnit);
        var minListingHq = listings.Where(l => l.Hq).MinBy(l => l.PricePerUnit);
        var (dc, region) = _worldToDcRegion.Get(worldId);
        if (minListingNq != null)
        {
            await cache.StringSetAsync(GetMinListingCacheKey(worldId, itemId, false), minListingNq.PricePerUnit, flags: CommandFlags.FireAndForget);
            await cache.SortedSetAddAsync(GetMinListingCacheKey(dc, itemId, false), worldId, minListingNq.PricePerUnit, flags: CommandFlags.FireAndForget);
            await cache.SortedSetAddAsync(GetMinListingCacheKey(region, itemId, false), worldId, minListingNq.PricePerUnit, flags: CommandFlags.FireAndForget);
        }
        else
        {
            await cache.KeyDeleteAsync(GetMinListingCacheKey(worldId, itemId, false), CommandFlags.FireAndForget);
            await cache.SortedSetRemoveAsync(GetMinListingCacheKey(dc, itemId, false), worldId, CommandFlags.FireAndForget);
            await cache.SortedSetRemoveAsync(GetMinListingCacheKey(region, itemId, false), worldId, CommandFlags.FireAndForget);
        }
        if (minListingHq != null)
        {
            await cache.StringSetAsync(GetMinListingCacheKey(worldId, itemId, true), minListingHq.PricePerUnit, flags: CommandFlags.FireAndForget);
            await cache.SortedSetAddAsync(GetMinListingCacheKey(dc, itemId, true), worldId, minListingHq.PricePerUnit, flags: CommandFlags.FireAndForget);
            await cache.SortedSetAddAsync(GetMinListingCacheKey(region, itemId, true), worldId, minListingHq.PricePerUnit, flags: CommandFlags.FireAndForget);
        }
        else
        {
            await cache.KeyDeleteAsync(GetMinListingCacheKey(worldId, itemId, true), CommandFlags.FireAndForget);
            await cache.SortedSetRemoveAsync(GetMinListingCacheKey(dc, itemId, true), worldId, CommandFlags.FireAndForget);
            await cache.SortedSetRemoveAsync(GetMinListingCacheKey(region, itemId, true), worldId, CommandFlags.FireAndForget);
        }
        await cache.StringSetAsync(GetUploadTimeCacheKey(worldId, itemId), uploadedAt.ToUnixTimeMilliseconds(), flags: CommandFlags.FireAndForget);
    }

    private static RedisKey GetMinListingCacheKey(object worldIdDcRegion, int itemId, bool hq) =>
        $"min-listing:{worldIdDcRegion}:{itemId}:{(hq ? "hq" : "nq")}";

    private static RedisKey GetUploadTimeCacheKey(int worldId, int itemId) =>
        $"upload-time:{worldId}:{itemId}";

    public async Task<MinListing> GetMinListing(int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.GetMinListing");

        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var (dc, region) = _worldToDcRegion.Get(worldId);
        var values = await cache.StringGetAsync(new[] { GetMinListingCacheKey(worldId, itemId, false), GetMinListingCacheKey(worldId, itemId, true) }, CommandFlags.PreferReplica);
        var nqPrice = values[0] != RedisValue.Null && values[0].TryParse(out int nq) ? new MinListing.Price(worldId, nq) : null;
        var hqPrice = values[1] != RedisValue.Null && values[1].TryParse(out int hq) ? new MinListing.Price(worldId, hq) : null;
        var dcMin = await GetMinListingForDcOrRegion(dc, itemId, cancellationToken);
        var regionMin = await GetMinListingForDcOrRegion(region, itemId, cancellationToken);
        return new MinListing
        {
            World = new MinListing.Entry(nqPrice, hqPrice),
            Dc = dcMin,
            Region = regionMin,
        };
    }

    public async Task<MinListing.Entry> GetMinListingForDcOrRegion(string dcOrRegion, int itemId, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.GetMinListingForDcOrRegion");

        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var minEntryNq = await cache.SortedSetRangeByScoreWithScoresAsync(GetMinListingCacheKey(dcOrRegion, itemId, false), take: 1, flags: CommandFlags.PreferReplica);
        var nqPrice = minEntryNq.Length > 0 && minEntryNq[0].Element.TryParse(out int worldIdNq) ? new MinListing.Price(worldIdNq, (int)minEntryNq[0].Score) : null;
        var minEntryHq = await cache.SortedSetRangeByScoreWithScoresAsync(GetMinListingCacheKey(dcOrRegion, itemId, true), take: 1, flags: CommandFlags.PreferReplica);
        var hqPrice = minEntryHq.Length > 0 && minEntryHq[0].Element.TryParse(out int worldIdHq) ? new MinListing.Price(worldIdHq, (int)minEntryHq[0].Score) : null;
        return new MinListing.Entry(nqPrice, hqPrice);
    }

    public async Task<IEnumerable<MarketItem>> GetCachedUploadTime(ICollection<MarketItemQuery> queries, CancellationToken cancellationToken)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.GetCachedUploadTime");

        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var result = await cache.StringGetAsync(queries.Select(q => GetUploadTimeCacheKey(q.WorldId, q.ItemId)).ToArray(), CommandFlags.PreferReplica);
        return result.Zip(queries, (v, q) => v != RedisValue.Null && v.TryParse(out long time)
            ? new MarketItem
            {
                ItemId = q.ItemId,
                WorldId = q.WorldId,
                LastUploadTime = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime,
            }
            : null)
            .Where(m => m != null);
    }

    public async Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveLive");

        // Try to fetch the listings from the cache
        activity?.AddEvent(new ActivityEvent("TryGetListingsFromCache"));
        var (success, cacheValue) = await TryGetListingsFromCache(query.WorldId, query.ItemId, cancellationToken);
        if (success)
        {
            return cacheValue;
        }

        // Query the database
        activity?.AddEvent(new ActivityEvent("NpgsqlCreateCommand"));
        await using var command = _dataSource.CreateCommand(
            $"SELECT {ListingColumns} FROM listing WHERE item_id = $1 AND world_id = $2 ORDER BY unit_price");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });

        try
        {
            activity?.AddEvent(new ActivityEvent("NpgsqlCommandExecuteReaderAsync"));
            await using var reader =
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var listings = await ReadListings(reader, cancellationToken);

            // Cache the result temporarily
            await StoreListingsInCache(query.WorldId, query.ItemId, listings, cancellationToken);

            if (Random.Shared.NextDouble() < 0.2)
            {
                // Record metric 20% of the time because this is a hot path
                RowsReadCount.Observe(listings.Count);
            }

            return listings;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (world={}, item={})", query.WorldId, query.ItemId);
            throw;
        }
    }

    public async Task<IDictionary<WorldItemPair, IList<Listing>>> RetrieveManyLive(ListingManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveManyLive");

        var worldIds = query.WorldIds.ToList();
        var itemIds = query.ItemIds.ToList();
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        var listings = new Dictionary<WorldItemPair, IList<Listing>>(worldItemPairs.Select(wip =>
            new KeyValuePair<WorldItemPair, IList<Listing>>(wip, new List<Listing>())));

        // Attempt to retrieve listings from the cache
        activity?.AddEvent(new ActivityEvent("TryGetListingsFromCacheMulti"));
        var cacheValues = await TryGetListingsFromCacheMulti(worldItemPairs, cancellationToken);
        if (cacheValues.Count == worldItemPairs.Count)
        {
            // Retrieved everything from the cache
            return cacheValues;
        }

        foreach (var (wip, cacheValue) in cacheValues)
        {
            listings[wip] = cacheValue;
            worldItemPairs.Remove(wip);
        }

        activity?.AddEvent(new ActivityEvent("NpgsqlCreateCommand"));
        var createCommandStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await using var command = _dataSource.CreateCommand(
            """
            SELECT t.listing_id, t.item_id, t.world_id, t.hq, t.on_mannequin, t.materia,
                   t.unit_price, t.quantity, t.dye_id, t.creator_name,
                   t.last_review_time, t.retainer_id, t.retainer_name, t.retainer_city_id,
                   t.uploaded_at, t.source
            FROM listing t
            WHERE t.item_id = ANY($1) AND t.world_id = ANY($2)
            """);
        var createCommandEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        CreateCommandDuration.Observe(createCommandEndTime - createCommandStartTime);
        command.Parameters.Add(new NpgsqlParameter<int[]>
        { TypedValue = worldItemPairs.Select(wip => wip.ItemId).Distinct().ToArray() });
        command.Parameters.Add(new NpgsqlParameter<int[]>
        { TypedValue = worldItemPairs.Select(wip => wip.WorldId).Distinct().ToArray() });

        try
        {
            activity?.AddEvent(new ActivityEvent("NpgsqlCommandExecuteReaderAsync"));
            using (ExecuteReaderDuration.NewTimer())
            {
                await using var reader =
                    await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    activity?.AddEvent(new ActivityEvent("NpgsqlReaderRead"));
                    var listingId = string.Intern(reader.GetString(0));
                    var itemId = reader.GetInt32(1);
                    var worldId = reader.GetInt32(2);
                    var worldItemPair = new WorldItemPair(worldId, itemId);

                    if (!listings.TryGetValue(worldItemPair, out var value))
                    {
                        value = new List<Listing>();
                        listings[worldItemPair] = value;
                    }

                    value.Add(new Listing
                    {
                        ListingId = listingId,
                        ItemId = itemId,
                        WorldId = worldId,
                        Hq = reader.GetBoolean(3),
                        OnMannequin = reader.GetBoolean(4),
                        Materia = ReadMateriaFromReader(reader),
                        PricePerUnit = reader.GetInt32(6),
                        Quantity = reader.GetInt32(7),
                        DyeId = reader.GetInt32(8),
                        // Large hashed IDs are interned as they will likely be reused when
                        // the same item is requested again
                        CreatorId = null,
                        CreatorName = reader.GetString(9),
                        LastReviewTime = reader.GetDateTime(10),
                        RetainerId = string.Intern(reader.GetString(11)),
                        RetainerName = reader.GetString(12),
                        RetainerCityId = reader.GetInt32(13),
                        SellerId = null,
                        UpdatedAt = reader.GetDateTime(14),
                        // Small strings, but with only a few possible values
                        Source = string.Intern(reader.GetString(15)),
                    });
                }
            }

            var result = listings.ToDictionary(
                kvp => kvp.Key,
                kvp => (IList<Listing>)kvp.Value.OrderBy(listing => listing.PricePerUnit).ToList());

            // Cache the results, except for things we already got from the cache
            activity?.AddEvent(new ActivityEvent("StoreListingsInCacheMulti"));
            var toCache = result.Where(r => !cacheValues.ContainsKey(r.Key))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value);
            await StoreListingsInCacheMulti(toCache, cancellationToken);

            if (Random.Shared.NextDouble() < 0.2)
            {
                // Record metric 20% of the time because this is a hot path
                RowsReadCount.Observe(result.Count - cacheValues.Count);
            }

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (worlds={}, items={})", string.Join(',', worldIds),
                string.Join(',', itemIds));
            throw;
        }
    }

    private async Task<(bool, IList<Listing>)> TryGetListingsFromCache(int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.TryGetListingsFromCache");
        var (localCached, localCacheResult) = await TryGetListingsFromLocalCache(worldId, itemId, cancellationToken);
        return localCached ? (true, localCacheResult) : (false, null);
    }

    private async Task<IDictionary<WorldItemPair, IList<Listing>>> TryGetListingsFromCacheMulti(
        IList<WorldItemPair> keys,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.TryGetListingsFromCacheMulti");

        // Attempt to retrieve listings from the local cache. Even though this is parallelizable, we'll run into
        // issues with task thrashing if we create task fan-out this deep in the request.
        var results = new Dictionary<WorldItemPair, IList<Listing>>();
        foreach (var wip in keys)
        {
            var (worldId, itemId) = wip;
            var (localCached, localCacheResult) =
                await TryGetListingsFromLocalCache(worldId, itemId, cancellationToken);
            if (localCached)
            {
                results[wip] = localCacheResult;
            }
        }

        return results;
    }

    private async Task StoreListingsInCache(int worldId, int itemId, IList<Listing> listings,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.StoreListingsInCache");
        await StoreListingsInLocalCache(worldId, itemId, listings, cancellationToken);
    }

    private async Task StoreListingsInCacheMulti(IDictionary<WorldItemPair, IList<Listing>> listings,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.StoreListingsInCacheMulti");
        foreach (var wip in listings.Keys)
        {
            await StoreListingsInLocalCache(wip.WorldId, wip.ItemId, listings[wip], cancellationToken);
        }
    }

    private async Task<(bool, IList<Listing>)> TryGetListingsFromLocalCache(int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.TryGetListingsFromLocalCache");

        var cacheKey = ListingsKey(worldId, itemId);
        var cacheValue = await _easyCachingProvider.GetAsync<IList<Listing>>(cacheKey, cancellationToken);
        if (cacheValue.HasValue)
        {
            LocalCacheHits.Inc();
            return (true, cacheValue.Value);
        }
        else
        {
            LocalCacheMisses.Inc();
            return (false, null);
        }
    }

    private async Task StoreListingsInLocalCache(int worldId, int itemId, IList<Listing> listings,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.StoreListingsInLocalCache");
        var cacheKey = ListingsKey(worldId, itemId);
        await _easyCachingProvider.SetAsync(cacheKey, listings, LocalListingsCacheTime, cancellationToken);
        LocalCacheUpdates.Inc();
    }

    private static string ListingsKey(int worldId, int itemId)
    {
        return $"listing5:{worldId}:{itemId}";
    }

    private static NpgsqlParameter ConvertMateriaToParameter(IList<Materia> materia)
    {
        var jArray = ConvertMateriaToJArray(materia);
        return jArray != null
            ? new NpgsqlParameter { Value = jArray, NpgsqlDbType = NpgsqlDbType.Jsonb }
            : new NpgsqlParameter { Value = DBNull.Value };
    }

    [CanBeNull]
    private static JArray ConvertMateriaToJArray(IList<Materia> materia)
    {
        if (materia is null || materia.Count == 0) return null;
        return materia
            .Select(m => new JObject { ["slot_id"] = m.SlotId, ["materia_id"] = m.MateriaId })
            .Aggregate(new JArray(), (array, o) =>
            {
                array.Add(o);
                return array;
            });
    }

    private static List<Materia> ReadMateriaFromReader(NpgsqlDataReader reader)
    {
        return reader.IsDBNull(5)
            ? new List<Materia>()
            : reader.GetFieldValue<List<Materia>>(5);
    }
}
