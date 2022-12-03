using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class MarketItemStore : IMarketItemStore
{
    private readonly ILogger<MarketItemStore> _logger;
    private readonly ICacheRedisMultiplexer _cache;
    private readonly ISession _scylla;
    private readonly IMapper _mapper;
    
    public MarketItemStore(ICluster scylla, ICacheRedisMultiplexer cache, ILogger<MarketItemStore> logger)
    {
        _cache = cache;
        _logger = logger;

        _scylla = scylla.Connect();
        _scylla.CreateKeyspaceIfNotExists("market_item");
        _scylla.ChangeKeyspace("market_item");
        var table = _scylla.GetTable<MarketItem>();
        table.CreateIfNotExists();

        _mapper = new Mapper(_scylla);
    }

    public async Task Insert(MarketItem marketItem, CancellationToken cancellationToken = default)
    {
        if (marketItem == null)
        {
            throw new ArgumentNullException(nameof(marketItem));
        }

        try
        {
            await _mapper.InsertAsync(marketItem);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
            throw;
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
            await _mapper.InsertAsync(marketItem);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert market item (world={WorldId}, item={ItemId})", marketItem.WorldId, marketItem.ItemId);
            throw;
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

    public async ValueTask<MarketItem> Retrieve(int worldId, int itemId, CancellationToken cancellationToken = default)
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
        var match = await _mapper.FirstOrDefaultAsync<MarketItem>("SELECT * FROM market_item WHERE item_id=? AND world_id=?", itemId, worldId);
        if (match == null)
        {
            return null;
        }
        
        // Cache the result
        try
        {
            await cache.StringSetAsync(cacheKey, match.LastUploadTime.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store MarketItem \"{MarketItemCacheKey}\" in cache (t={LastUploadTime})", cacheKey, match.LastUploadTime);
            throw;
        }

        return match;
    }

    private static string GetCacheKey(int worldId, int itemId)
    {
        return $"market-item:{worldId}:{itemId}";
    }
}