using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Logging;
using Npgsql;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly string _connectionString;
    private readonly IMemcachedCluster _memcached;
    private readonly ILogger<MarketItemStore> _logger;

    public MarketItemStore(string connectionString, IMemcachedCluster memcached, ILogger<MarketItemStore> logger)
    {
        _connectionString = connectionString;
        _memcached = memcached;
        _logger = logger;
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command =
            new NpgsqlCommand(
                "INSERT INTO market_item (world_id, item_id, updated) VALUES ($1, $2, $3)", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.ItemId) },
                    new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime },
                },
            };

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e) when (e.ConstraintName == "PK_market_item_item_id_world_id")
        {
            // Race condition; unique constraint violated
        }

        // Write through to the cache
        var cache = _memcached.GetClient();
        var cacheData = JsonSerializer.Serialize(marketItem.Clone());
        await cache.SetAsync(GetCacheKey(marketItem.WorldId, marketItem.ItemId), cacheData);
    }

    public async Task Update(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        if (await Retrieve(marketItem.WorldId, marketItem.ItemId, cancellationToken) == null)
        {
            await Insert(marketItem, cancellationToken);
            return;
        }

        await using var command =
            new NpgsqlCommand(
                "UPDATE market_item SET updated = $1 WHERE world_id = $2 AND item_id = $3", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<DateTime> { TypedValue = marketItem.LastUploadTime },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(marketItem.ItemId) },
                },
            };
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Write through to the cache
        var cache = _memcached.GetClient();
        var cacheData = JsonSerializer.Serialize(marketItem.Clone());
        await cache.SetAsync(GetCacheKey(marketItem.WorldId, marketItem.ItemId), cacheData);
    }

    public async ValueTask<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        // Try to fetch data from the cache
        var cache = _memcached.GetClient();
        var cacheData1 = await cache.GetWithResultAsync<string>(GetCacheKey(worldId, itemId));
        if (cacheData1.Success)
        {
            try
            {
                var cacheObject = JsonSerializer.Deserialize<MarketItem>(cacheData1.Value);
                if (cacheObject != null)
                {
                    return cacheObject;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to deserialize object: {JsonData}", cacheData1.Value);
            }
        }

        // Fetch data from the database
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(
                "SELECT world_id, item_id, updated FROM market_item WHERE world_id = $1 AND item_id = $2", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(worldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(itemId) },
                },
            };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!reader.HasRows)
        {
            return null;
        }

        await reader.ReadAsync(cancellationToken);

        var newItem = new MarketItem
        {
            WorldId = Convert.ToUInt32(reader.GetInt32(0)),
            ItemId = Convert.ToUInt32(reader.GetInt32(1)),
            LastUploadTime = (DateTime)reader.GetValue(2),
        };

        // Cache the result
        var cacheData2 = JsonSerializer.Serialize(newItem);
        await cache.SetAsync(GetCacheKey(worldId, itemId), cacheData2);

        return newItem;
    }

    private static string GetCacheKey(uint worldId, uint itemId)
    {
        return $"market-item:{worldId}:{itemId}";
    }
}