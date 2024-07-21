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

    private static readonly TimeSpan LocalListingsCacheTime = TimeSpan.FromMinutes(5);

    private readonly ILogger<ListingStore> _logger;
    private readonly IEasyCachingProvider _easyCachingProvider;
    private readonly NpgsqlDataSource _dataSource;

    public ListingStore(NpgsqlDataSource dataSource, IEasyCachingProvider easyCachingProvider,
        ILogger<ListingStore> logger)
    {
        _easyCachingProvider = easyCachingProvider;
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task DeleteLive(ListingQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.DeleteLive");
        await using var command = _dataSource.CreateCommand("DELETE FROM listing WHERE item_id = $1 AND world_id = $2");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });
        try
        {
            var rowsUpdated = await command.ExecuteNonQueryAsync(cancellationToken);
            activity?.AddTag("rowsUpdated", rowsUpdated);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete listings (world={}, item={})", query.WorldId,
                query.ItemId);
            throw;
        }
    }

    public async Task ReplaceLive(IEnumerable<Listing> listings, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.ReplaceLive");
        var rowsUpdated = 0;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        // Get the current timestamp for the batch
        var uploadedAt = DateTimeOffset.Now;

        // Listings are grouped for better exceptions if a batch fails; exceptions can be
        // filtered by world and item.
        var groupedListings = listings.GroupBy(l => new WorldItemPair(l.WorldId, l.ItemId));
        foreach (var listingGroup in groupedListings)
        {
            var (worldID, itemID) = listingGroup.Key;

            // Npgsql batches have an implicit transaction around them
            // https://www.npgsql.org/doc/basic-usage.html#batching
            await using var batch = new NpgsqlBatch(connection);
            batch.BatchCommands.Add(new NpgsqlBatchCommand("DELETE FROM listing WHERE item_id = $1 AND world_id = $2")
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = itemID },
                    new NpgsqlParameter<int> { TypedValue = worldID },
                },
            });

            foreach (var listing in listingGroup)
            {
                // If a listing is uploaded multiple times in separate uploads, it
                // can already be in the database, causing a conflict. To handle that,
                // we just update the existing record and ensure that it's made live
                // again. It's not clear to me what happens on the game servers when
                // a listing is updated. Until we have more data, I'm assuming that
                // all updates are the same as new listings.
                batch.BatchCommands.Add(new NpgsqlBatchCommand(
                    """
                    INSERT INTO listing
                    (listing_id, item_id, world_id, hq, on_mannequin, materia, unit_price, quantity, dye_id, creator_id,
                     creator_name, last_review_time, retainer_id, retainer_name, retainer_city_id, seller_id, uploaded_at,
                     source)
                    VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15, $16, $17, $18)
                    ON CONFLICT (listing_id) DO NOTHING;
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
                        new NpgsqlParameter<string> { TypedValue = listing.CreatorId },
                        new NpgsqlParameter<string> { TypedValue = listing.CreatorName },
                        new NpgsqlParameter<DateTime> { TypedValue = listing.LastReviewTime },
                        new NpgsqlParameter<string> { TypedValue = listing.RetainerId },
                        new NpgsqlParameter<string> { TypedValue = listing.RetainerName },
                        new NpgsqlParameter<int> { TypedValue = listing.RetainerCityId },
                        new NpgsqlParameter<string> { TypedValue = listing.SellerId },
                        new NpgsqlParameter<DateTime> { TypedValue = uploadedAt.UtcDateTime },
                        new NpgsqlParameter<string> { TypedValue = listing.Source },
                    },
                });
            }

            try
            {
                rowsUpdated += await batch.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                activity?.AddTag("rowsUpdated", rowsUpdated);
                _logger.LogError(e, "Failed to insert listings (world={}, item={})", worldID,
                    itemID);
                throw;
            }
        }

        activity?.AddTag("rowsUpdated", rowsUpdated);
    }

    public async Task<IEnumerable<Listing>> RetrieveLive(ListingQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ListingStore.RetrieveLive");

        // Try to fetch the listings from the cache
        var (success, cacheValue) = await TryGetListingsFromCache(query.WorldId, query.ItemId, cancellationToken);
        if (success)
        {
            return cacheValue;
        }

        // Query the database
        await using var command = _dataSource.CreateCommand(
            """
            SELECT t.listing_id, t.item_id, t.world_id, t.hq, t.on_mannequin, t.materia,
                   t.unit_price, t.quantity, t.dye_id, t.creator_id, t.creator_name,
                   t.last_review_time, t.retainer_id, t.retainer_name, t.retainer_city_id,
                   t.seller_id, t.uploaded_at, t.source
            FROM listing t
            WHERE t.item_id = $1 AND t.world_id = $2
            ORDER BY unit_price
            """);
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var listings = new List<Listing>();
            while (await reader.ReadAsync(cancellationToken))
            {
                listings.Add(new Listing
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
                    CreatorId = string.Intern(reader.GetString(9)),
                    CreatorName = reader.GetString(10),
                    LastReviewTime = reader.GetDateTime(11),
                    RetainerId = string.Intern(reader.GetString(12)),
                    RetainerName = reader.GetString(13),
                    RetainerCityId = reader.GetInt32(14),
                    SellerId = string.Intern(reader.GetString(15)),
                    UpdatedAt = reader.GetDateTime(16),
                    Source = string.Intern(reader.GetString(17)),
                });
            }

            // Cache the result temporarily
            await StoreListingsInCache(query.WorldId, query.ItemId, listings, cancellationToken);

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
                   t.unit_price, t.quantity, t.dye_id, t.creator_id, t.creator_name,
                   t.last_review_time, t.retainer_id, t.retainer_name, t.retainer_city_id,
                   t.seller_id, t.uploaded_at, t.source
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
                        CreatorId = string.Intern(reader.GetString(9)),
                        CreatorName = reader.GetString(10),
                        LastReviewTime = reader.GetDateTime(11),
                        RetainerId = string.Intern(reader.GetString(12)),
                        RetainerName = reader.GetString(13),
                        RetainerCityId = reader.GetInt32(14),
                        SellerId = string.Intern(reader.GetString(15)),
                        UpdatedAt = reader.GetDateTime(16),
                        // Small strings, but with only a few possible values
                        Source = string.Intern(reader.GetString(17)),
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
            : ConvertMateriaFromJArray(reader.GetFieldValue<JArray>(5));
    }

    private static List<Materia> ConvertMateriaFromJArray(IEnumerable<JToken> materia)
    {
        return materia
            .Select(m => new Materia { SlotId = m["slot_id"].Value<int>(), MateriaId = m["materia_id"].Value<int>() })
            .ToList();
    }
}