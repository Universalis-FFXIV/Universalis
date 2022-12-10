using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStore : ISaleStore
{
    // The maximum number of cached sales, per item, per world
    private const int MaxCachedSales = 50;

    private readonly ICacheRedisMultiplexer _cache;
    private readonly ILogger<SaleStore> _logger;
    private readonly ISession _scylla;
    private readonly IMapper _mapper;

    private readonly PreparedStatement _insertStatement;

    public SaleStore(ICluster scylla, ICacheRedisMultiplexer cache, ILogger<SaleStore> logger)
    {
        _cache = cache;
        _logger = logger;

        _scylla = scylla.Connect();
        _scylla.CreateKeyspaceIfNotExists("sale");
        _scylla.ChangeKeyspace("sale");
        var table = _scylla.GetTable<Sale>();
        table.CreateIfNotExists();

        _mapper = new Mapper(_scylla);

        _insertStatement = _scylla.Prepare("" +
            "INSERT INTO sale" +
            "(id, sale_time, item_id, world_id, buyer_name, hq, on_mannequin, quantity, unit_price, uploader_id)" +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
    }

    public async Task Insert(Sale sale, CancellationToken cancellationToken = default)
    {
        if (sale == null)
        {
            throw new ArgumentNullException(nameof(sale));
        }

        if (sale.BuyerName == null)
        {
            throw new ArgumentException("Sale buyer name may not be null.", nameof(sale));
        }

        if (sale.Quantity == null)
        {
            throw new ArgumentException("Sale quantity may not be null.", nameof(sale));
        }

        if (sale.OnMannequin == null)
        {
            throw new ArgumentException("Mannequin state may not be null.", nameof(sale));
        }

        try
        {
            await _mapper.InsertAsync(sale);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert sale (world={WorldId}, item={ItemId})", sale.WorldId, sale.ItemId);
            throw;
        }

        // Purge the cache
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
        var cacheIndexKey = GetIndexCacheKey(sale.WorldId, sale.ItemId);
        await cache.KeyDeleteAsync(cacheIndexKey, CommandFlags.FireAndForget);
    }

    public async Task InsertMany(IEnumerable<Sale> sales, CancellationToken cancellationToken = default)
    {
        if (sales == null)
        {
            throw new ArgumentNullException(nameof(sales));
        }

        var purgedCaches = new Dictionary<string, bool>();
        var batch = new BatchStatement();
        foreach (var sale in sales)
        {
            var bound = _insertStatement.Bind(
                sale.Id,
                sale.SaleTime,
                sale.ItemId,
                sale.WorldId,
                sale.BuyerName,
                sale.Hq,
                sale.OnMannequin,
                sale.Quantity,
                sale.PricePerUnit,
                sale.UploaderIdHash);
            batch.Add(bound);
            
            // Purge the cache
            var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
            var cacheIndexKey = GetIndexCacheKey(sale.WorldId, sale.ItemId);
            if (!purgedCaches.TryGetValue(cacheIndexKey, out var purged) || !purged)
            {
                await cache.KeyDeleteAsync(cacheIndexKey, CommandFlags.FireAndForget);
                purgedCaches[cacheIndexKey] = true;
            }
        }

        try
        {
            await _scylla.ExecuteAsync(batch);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to insert sales");
            throw;
        }
    }

    public async Task<IEnumerable<Sale>> RetrieveBySaleTime(int worldId, int itemId, int count, DateTime? from = null, CancellationToken cancellationToken = default)
    {
        var sales = Enumerable.Empty<Sale>();
        if (count == 0)
        {
            return sales;
        }

        // Fetch data from the database
        var timestamp = from == null ? 0 : new DateTimeOffset(from.Value).ToUnixTimeMilliseconds();
        try
        {
            sales = await _mapper.FetchAsync<Sale>("SELECT * FROM sale WHERE item_id=? AND world_id=? AND sale_time>=? ORDER BY sale_time DESC LIMIT ?", itemId, worldId, timestamp, count);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve sales (world={WorldId}, item={ItemId})", worldId, itemId);
            throw;
        }

        return sales
            .Select(sale =>
            {
                sale.SaleTime = DateTime.SpecifyKind(sale.SaleTime, DateTimeKind.Utc);
                return sale;
            });
    }

    public async Task<long> RetrieveUnitTradeVolume(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Check if the data needed is cached
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
        var cacheKey = GetUnitTradeVolumeCacheKey(worldId, itemId);
        try
        {
            var cachedFromRaw = await cache.HashGetAsync(cacheKey, "cached-from", flags: CommandFlags.PreferReplica);

            // It's fine if new data is missing for a bit, but needing older data should be treated as a cache miss
            if (DateTime.TryParse(cachedFromRaw, out var cachedFrom) && cachedFrom <= from)
            {
                var saleVolumes = await cache.HashGetAllAsync(cacheKey, flags: CommandFlags.PreferReplica);
                return AggregateHashVolume(saleVolumes, from, to);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{TradeVolumeCacheKey}\"", cacheKey);
        }

        // Request the sale velocity for the allowed intervals
        // TODO: Don't re-request cached values
        var result = await GetDailyUnitsTraded(worldId, itemId, from, to, cancellationToken);

        // Cache it
        try
        {
            await CacheTradeVolume(cache, cacheKey, result, from, to);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store unit trade volumes \"{TradeVolumeCacheKey}\" in cache", cacheKey);
        }

        return result.Select(e => e.Value).Sum();
    }

    public async Task<long> RetrieveGilTradeVolume(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // Check if the data needed is cached
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
        var cacheKey = GetGilTradeVolumeCacheKey(worldId, itemId);
        try
        {
            var cachedFromRaw = await cache.HashGetAsync(cacheKey, "cached-from", flags: CommandFlags.PreferReplica);

            // It's fine if new data is missing for a bit, but needing older data should be treated as a cache miss
            if (DateTime.TryParse(cachedFromRaw, out var cachedFrom) && cachedFrom <= from)
            {
                var saleVolumes = await cache.HashGetAllAsync(cacheKey, flags: CommandFlags.PreferReplica);
                return AggregateHashVolume(saleVolumes, from, to);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{TradeVolumeCacheKey}\"", cacheKey);
        }

        // Request the sale velocity for the allowed intervals
        // TODO: Don't re-request cached values
        var result = await GetDailyGilTraded(worldId, itemId, from, to, cancellationToken);

        // Cache it
        try
        {
            await CacheTradeVolume(cache, cacheKey, result, from, to);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store unit trade volumes \"{TradeVolumeCacheKey}\" in cache", cacheKey);
        }

        return result.Select(e => e.Value).Sum();
    }

    private static async Task CacheTradeVolume(IDatabase cache, string cacheKey, IDictionary<DateTime, int> hashData, DateTime from, DateTime to)
    {
        var hash = hashData.Select(kvp => new HashEntry(kvp.Key.ToString(), kvp.Value)).ToList();
        hash.Add(new HashEntry("cached-from", from.ToString()));
        hash.Add(new HashEntry("cached-to", to.ToString()));
        await cache.HashSetAsync(cacheKey, hash.ToArray(), CommandFlags.FireAndForget);
        await cache.KeyExpireAsync(cacheKey, TimeSpan.FromHours(1), CommandFlags.FireAndForget);
    }

    private Task<IDictionary<DateTime, int>> GetDailyUnitsTraded(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla or DynamoDB
        return Task.FromResult<IDictionary<DateTime, int>>(new Dictionary<DateTime, int>());
    }

    private Task<IDictionary<DateTime, int>> GetDailyGilTraded(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla or DynamoDB
        return Task.FromResult<IDictionary<DateTime, int>>(new Dictionary<DateTime, int>());
    }

    private long AggregateHashVolume(IEnumerable<HashEntry> hash, DateTime from, DateTime to)
    {
        return hash
            .Where(e => DateTime.TryParse(e.Name, out _))
            .Select(e => new KeyValuePair<DateTime, long>(DateTime.Parse(e.Name), (long)e.Value))
            .Where(kvp => kvp.Key >= from)
            .Where(kvp => kvp.Key <= to)
            .Select(kvp => kvp.Value)
            .Sum();
    }

    private static int GetValueInt32(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? (int)hash[key] : 0;
    }

    private static bool GetValueBool(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) && (bool)hash[key];
    }

    private static string GetValueString(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? hash[key] : "";
    }

    private static string GetUnitTradeVolumeCacheKey(int worldId, int itemId)
    {
        return $"sale-volume:{worldId}:{itemId}";
    }

    private static string GetGilTradeVolumeCacheKey(int worldId, int itemId)
    {
        return $"sale-volume-gil:{worldId}:{itemId}";
    }

    private static string GetIndexCacheKey(int worldId, int itemId)
    {
        return $"sale-index:{worldId}:{itemId}";
    }

    private static string GetSaleCacheKey(Guid saleId)
    {
        return $"sale:{saleId}";
    }
}