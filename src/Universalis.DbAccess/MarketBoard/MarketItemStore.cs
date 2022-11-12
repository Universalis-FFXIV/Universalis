using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly ICacheRedisMultiplexer _cache;
    private readonly ILogger<MarketItemStore> _logger;
    private readonly string _connectionString;

    public MarketItemStore(string connectionString, ICacheRedisMultiplexer cache, ILogger<MarketItemStore> logger)
    {
        _connectionString = connectionString;
        _cache = cache;
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
        var cache = _cache.GetDatabase(RedisDatabases.Cache.MarketItem);
        var cacheKey = GetCacheKey(marketItem.WorldId, marketItem.ItemId);
        try
        {
            await cache.StringSetAsync(cacheKey, marketItem.LastUploadTime.ToString(), flags: CommandFlags.FireAndForget);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store MarketItem \"{MarketItemCacheKey}\" in cache", cacheKey);
        }
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
        var cache = _cache.GetDatabase(RedisDatabases.Cache.MarketItem);
        var cacheKey = GetCacheKey(marketItem.WorldId, marketItem.ItemId);
        try
        {
            await cache.StringSetAsync(cacheKey, marketItem.LastUploadTime.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store MarketItem \"{MarketItemCacheKey}\" in cache", cacheKey);
        }
    }

    public async ValueTask<MarketItem> Retrieve(uint worldId, uint itemId, CancellationToken cancellationToken = default)
    {
        // Try to retrieve data from the cache
        var cache = _cache.GetDatabase(RedisDatabases.Cache.MarketItem);
        var cacheKey = GetCacheKey(worldId, itemId);
        try
        {
            if (await cache.KeyExistsAsync(cacheKey))
            {
                var timestamp = await cache.StringGetAsync(cacheKey);
                if (DateTime.TryParse(timestamp, out var t))
                {
                    return new MarketItem
                    {
                        WorldId = worldId,
                        ItemId = itemId,
                        LastUploadTime = t,
                    };
                }
                else
                {
                    _logger.LogWarning("Failed to parse timestamp \"{RedisValue}\" for cached MarketItem \"{MarketItemCacheKey}\"", timestamp, cacheKey);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve MarketItem \"{MarketItemCacheKey}\" from the cache", cacheKey);
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
        try
        {
            await cache.StringSetAsync(cacheKey, newItem.LastUploadTime.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store MarketItem \"{MarketItemCacheKey}\" in cache", cacheKey);
        }

        return newItem;
    }

    private static string GetCacheKey(uint worldId, uint itemId)
    {
        return $"market-item:{worldId}:{itemId}";
    }
}