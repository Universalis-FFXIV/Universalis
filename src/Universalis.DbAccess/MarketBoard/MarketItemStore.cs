using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MemoryPack;
using Microsoft.Extensions.Logging;
using Npgsql;
using Prometheus;
using StackExchange.Redis;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private static readonly Counter CachePurges =
        Prometheus.Metrics.CreateCounter("universalis_market_item_cache_purge", "");

    private static readonly Counter CacheHits =
        Prometheus.Metrics.CreateCounter("universalis_market_item_cache_hit", "");

    private static readonly Counter
        CacheMisses = Prometheus.Metrics.CreateCounter("universalis_market_item_cache_miss", "");

    private static readonly Counter CacheUpdates =
        Prometheus.Metrics.CreateCounter("universalis_market_item_cache_update", "");

    private static readonly Counter CacheTimeouts =
        Prometheus.Metrics.CreateCounter("universalis_market_item_cache_timeout", "");

    private static readonly TimeSpan MarketItemCacheTime = TimeSpan.FromMinutes(10);

    private readonly ILogger<MarketItemStore> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ICacheRedisMultiplexer _cache;

    public MarketItemStore(NpgsqlDataSource dataSource, ICacheRedisMultiplexer cache, ILogger<MarketItemStore> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
        _cache = cache;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.Insert");

        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var command =
            _dataSource.CreateCommand(
                "INSERT INTO market_item (item_id, world_id, updated) VALUES ($1, $2, $3) ON CONFLICT (item_id, world_id) DO UPDATE SET updated = $3");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = marketItem.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = marketItem.WorldId });
        command.Parameters.Add(new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            // Purge the cache
            var db = _cache.GetDatabase(RedisDatabases.Cache.Listings);
            var cacheKey = MarketItemKey(marketItem.WorldId, marketItem.ItemId);
            await db.KeyDeleteAsync(cacheKey, CommandFlags.FireAndForget);
            CachePurges.Inc();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={}, item={})", marketItem.WorldId,
                marketItem.ItemId);
            throw;
        }
    }

    public async ValueTask<MarketItem> Retrieve(MarketItemQuery query, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.Retrieve");

        // Try to fetch the market item from the cache
        var (success, cacheValue) = await TryGetMarketItemFromCache(query.WorldId, query.ItemId, cancellationToken);
        if (success)
        {
            return cacheValue;
        }

        await using var command =
            _dataSource.CreateCommand("SELECT updated FROM market_item WHERE item_id = $1 AND world_id = $2");
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.ItemId });
        command.Parameters.Add(new NpgsqlParameter<int> { TypedValue = query.WorldId });

        try
        {
            var lastUploadTime = (DateTime?)await command.ExecuteScalarAsync(cancellationToken);
            if (!lastUploadTime.HasValue)
            {
                return null;
            }

            var marketItem = new MarketItem
            {
                ItemId = query.ItemId,
                WorldId = query.WorldId,
                LastUploadTime = lastUploadTime.Value,
            };

            // Cache the result temporarily
            await StoreMarketItemInCache(query.WorldId, query.ItemId, marketItem);
            return marketItem;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve market item (world={}, item={})", query.WorldId, query.ItemId);
            throw;
        }
    }

    public async ValueTask<IEnumerable<MarketItem>> RetrieveMany(MarketItemManyQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.RetrieveMany");

        var worldIds = query.WorldIds.ToList();
        var itemIds = query.ItemIds.ToList();
        var worldItemTuples = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => (worldId, itemId)))
            .ToList();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(connection);

        foreach (var (worldId, itemId) in worldItemTuples)
        {
            batch.BatchCommands.Add(
                new NpgsqlBatchCommand("SELECT updated FROM market_item WHERE item_id = $1 AND world_id = $2")
                {
                    Parameters =
                    {
                        new NpgsqlParameter<int> { TypedValue = itemId },
                        new NpgsqlParameter<int> { TypedValue = worldId },
                    },
                });
        }

        try
        {
            await using var reader =
                await batch.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            var marketItemRecords = new List<MarketItem>();
            var batchesRead = 0;
            do
            {
                var (worldId, itemId) = worldItemTuples[batchesRead];
                if (await reader.ReadAsync(cancellationToken))
                {
                    marketItemRecords.Add(new MarketItem
                    {
                        ItemId = itemId,
                        WorldId = worldId,
                        LastUploadTime = reader.GetDateTime(0),
                    });
                }

                batchesRead++;
                await reader.NextResultAsync(cancellationToken);
            } while (batchesRead != worldItemTuples.Count);

            return marketItemRecords;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve listings (worlds={}, items={})", string.Join(',', worldIds),
                string.Join(',', itemIds));
            throw;
        }
    }

    private async Task<(bool, MarketItem)> TryGetMarketItemFromCache(int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.TryGetMarketItemFromCache");
        var db = _cache.GetDatabase(RedisDatabases.Cache.Listings);
        var cacheKey = MarketItemKey(worldId, itemId);

        try
        {
            var cacheValue = await db.StringGetAsync(cacheKey, CommandFlags.PreferReplica)
                .WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);
            if (cacheValue != RedisValue.Null)
            {
                CacheHits.Inc();
                return (true, DeserializeMarketItem(cacheValue));
            }
            else
            {
                CacheMisses.Inc();
                return (false, null);
            }
        }
        catch (TimeoutException)
        {
            CacheTimeouts.Inc();
            return (false, null);
        }
        catch (OperationCanceledException)
        {
            CacheTimeouts.Inc();
            return (false, null);
        }
    }

    private async Task StoreMarketItemInCache(int worldId, int itemId, MarketItem marketItem)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketItemStore.StoreMarketItemInCache");
        var db = _cache.GetDatabase(RedisDatabases.Cache.Listings);
        var cacheKey = MarketItemKey(worldId, itemId);
        await db.StringSetAsync(cacheKey, SerializeMarketItem(marketItem), MarketItemCacheTime, When.Always,
            CommandFlags.FireAndForget);
        CacheUpdates.Inc();
    }

    private static string MarketItemKey(int worldId, int itemId)
    {
        return $"market_item:{worldId}:{itemId}";
    }

    private static MarketItem DeserializeMarketItem(byte[] value)
    {
        return MemoryPackSerializer.Deserialize<MarketItem>(value);
    }

    private static byte[] SerializeMarketItem(MarketItem marketItem)
    {
        return MemoryPackSerializer.Serialize(marketItem);
    }
}