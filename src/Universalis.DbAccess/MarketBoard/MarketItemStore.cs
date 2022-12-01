using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Cassandra;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly ILogger<MarketItemStore> _logger;
    private readonly ICacheRedisMultiplexer _cache;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly ISession _scylla;
    private readonly DynamoDBContext _ddbContext;

    private readonly PreparedStatement _retrieveStatement;

    public MarketItemStore(ICluster scylla, IAmazonDynamoDB dynamoDb, ICacheRedisMultiplexer cache, ILogger<MarketItemStore> logger)
    {
        _dynamoDb = dynamoDb;
        _ddbContext = new DynamoDBContext(_dynamoDb);
        _cache = cache;
        _logger = logger;

        _scylla = scylla.Connect("alternator_market_item");
        _retrieveStatement = _scylla.Prepare("SELECT \":attrs\" FROM market_item WHERE world_id=? AND item_id=? LIMIT 1");
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        try
        {
            await _ddbContext.SaveAsync(marketItem, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
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

        try
        {
            await _ddbContext.SaveAsync(marketItem, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
        }

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
        var res = await _scylla.ExecuteAsync(_retrieveStatement.Bind(itemId, worldId));
        var data = res.FirstOrDefault();
        if (data == null)
        {
            return null;
        }

        var attrs = data.GetValue<Dictionary<string, string>>(0);
        if (!attrs.TryGetValue("last_upload_time", out var s) || !long.TryParse(s, out var lastUploadTime))
        {
            return null;
        }

        var match = new MarketItem
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTime = DateTimeOffset.FromUnixTimeMilliseconds(lastUploadTime).UtcDateTime,
        };

        // Cache the result
        try
        {
            await cache.StringSetAsync(cacheKey, match.LastUploadTime.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store MarketItem \"{MarketItemCacheKey}\" in cache (t={LastUploadTime})", cacheKey, match.LastUploadTime);
        }

        return match;
    }

    private static string GetCacheKey(uint worldId, uint itemId)
    {
        return $"market-item:{worldId}:{itemId}";
    }
}