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
using Condition = StackExchange.Redis.Condition;

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
        // Try retrieving data from the cache
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
        var cacheIndexKey = GetIndexCacheKey(worldId, itemId);
        if (from == null && count <= MaxCachedSales)
        {
            var cachedSales = await FetchSalesFromCache(cache, cacheIndexKey, worldId, itemId, count);
            if (cachedSales.Any())
            {
                return cachedSales;
            }
        }


        var sales = new List<Sale>();
        if (count == 0)
        {
            return sales;
        }

        // Fetch data from the database
        var timestamp = from == null ? 0 : new DateTimeOffset(from.Value).ToUnixTimeMilliseconds();
        byte[] pagingState = null;
        do
        {
            IPage<Sale> page;
            try
            {
                page = await _mapper.FetchPageAsync<Sale>(Cql.New("SELECT * FROM sale WHERE item_id=? AND world_id=? AND sale_time>=?", new object[] { itemId, worldId, timestamp })
                    .WithOptions(options => options
                        .SetConsistencyLevel(ConsistencyLevel.One)
                        .SetSerialConsistencyLevel(ConsistencyLevel.One)
                        .SetPageSize(50)
                        .SetPagingState(pagingState)));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to retrieve sales (world={WorldId}, item={ItemId})", worldId, itemId);
                throw;
            }

            if (page.Count == 0)
            {
                break;
            }

            pagingState = page.PagingState;
            foreach (var nextSale in page)
            {
                if (sales.Count >= count)
                {
                    break;
                }

                if (sales.Contains(nextSale))
                {
                    continue;
                }

                sales.Add(nextSale);
            }
        } while (sales.Count < count);

        var results = sales
            .Take(count)
            .OrderByDescending(sale => sale.SaleTime)
            .ToList();

        // Store the results in the cache
        if (results.Count >= MaxCachedSales)
        {
            await CacheSales(cache, cacheIndexKey, results);
        }

        return results;
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

    private async Task<IDictionary<DateTime, int>> GetDailyUnitsTraded(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla or DynamoDB
        return new Dictionary<DateTime, int>();
    }

    private async Task<IDictionary<DateTime, int>> GetDailyGilTraded(int worldId, int itemId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla or DynamoDB
        return new Dictionary<DateTime, int>();
    }

    private async Task<IList<Sale>> FetchSalesFromCache(IDatabase cache, string cacheIndexKey, int worldId, int itemId, int count)
    {
        try
        {
            if (await cache.KeyExistsAsync(cacheIndexKey, CommandFlags.PreferReplica))
            {
                var cachedSaleIds = await cache.ListRangeAsync(cacheIndexKey, flags: CommandFlags.PreferReplica);
                var cachedSaleTasks = cachedSaleIds
                    .Take(count)
                    .Select(saleId => Guid.Parse(saleId))
                    .Select(async saleId =>
                    {
                        var cacheKey = GetSaleCacheKey(saleId);
                        return (saleId, await cache.HashGetAllAsync(cacheKey, CommandFlags.PreferReplica));
                    });
                var cachedSales = await Task.WhenAll(cachedSaleTasks);
                return cachedSales
                    .Select(cachedSale =>
                    {
                        var (saleId, sale) = cachedSale;
                        return (saleId, sale.ToDictionary());
                    })
                    .Select(cachedSale =>
                    {
                        var (saleId, sale) = cachedSale;
                        return ParseSaleFromHash(saleId, worldId, itemId, sale);
                    })
                    .OrderByDescending(sale => sale.SaleTime)
                    .ToList();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve sales from cache \"{SaleIndexCacheKey}\"", cacheIndexKey);
        }

        return new List<Sale>();
    }

    private async Task CacheSales(IDatabase cache, string cacheIndexKey, IEnumerable<Sale> sales)
    {
        var salesTrimmed = sales.Take(MaxCachedSales).ToList();
        var saleIds = salesTrimmed.Select(sale => sale.Id.ToString());
        try
        {
            // Add all of the sales
            await Task.WhenAll(salesTrimmed.Select(async sale =>
            {
                var saleKey = GetSaleCacheKey(sale.Id);
                await cache.HashSetAsync(saleKey, new[]
                {
                    new HashEntry("hq", sale.Hq),
                    new HashEntry("ppu", sale.PricePerUnit),
                    new HashEntry("q", sale.Quantity),
                    new HashEntry("bn", sale.BuyerName),
                    new HashEntry("t", sale.SaleTime.ToString()),
                    new HashEntry("uid", sale.UploaderIdHash),
                    new HashEntry("mann", sale.OnMannequin)
                });
                await cache.KeyExpireAsync(saleKey, TimeSpan.FromMinutes(5), flags: CommandFlags.FireAndForget);
            }));

            // Update the index
            var s0 = await cache.ListGetByIndexAsync(cacheIndexKey, 0);
            var tx = cache.CreateTransaction();
            tx.AddCondition(Condition.ListIndexEqual(cacheIndexKey, 0, s0));
            _ = tx.ListLeftPushAsync(cacheIndexKey, saleIds.Select(id => (RedisValue)id).ToArray());
            _ = tx.KeyExpireAsync(cacheIndexKey, TimeSpan.FromMinutes(5));
            await tx.ExecuteAsync(CommandFlags.FireAndForget);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store sales in cache \"{SaleIndexCacheKey}\"", cacheIndexKey);
        }
    }

    private Sale ParseSaleFromHash(Guid saleId, int worldId, int itemId, IDictionary<RedisValue, RedisValue> hash)
    {
        var saleTimeStr = GetValueString(hash, "t");
        if (!DateTime.TryParse(saleTimeStr, out var saleTime))
        {
            saleTime = default;
            _logger.LogWarning("Failed to parse sale time  \"{RedisValue}\" for cached sale \"{SaleId}\", using default value", saleTimeStr, saleId);
        }

        return new Sale
        {
            Id = saleId,
            WorldId = worldId,
            ItemId = itemId,
            Hq = GetValueBool(hash, "hq"),
            PricePerUnit = GetValueInt32(hash, "ppu"),
            Quantity = GetValueInt32(hash, "q"),
            BuyerName = GetValueString(hash, "bn"),
            SaleTime = saleTime,
            UploaderIdHash = GetValueString(hash, "uid"),
            OnMannequin = GetValueBool(hash, "mann"),
        };
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