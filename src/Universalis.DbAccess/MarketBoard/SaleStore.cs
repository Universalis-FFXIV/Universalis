using Microsoft.Extensions.Logging;
using Npgsql;
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

    private readonly string _connectionString;
    private readonly ICacheRedisMultiplexer _cache;
    private readonly ILogger<SaleStore> _logger;

    public SaleStore(string connectionString, ICacheRedisMultiplexer cache, ILogger<SaleStore> logger)
    {
        _connectionString = connectionString;
        _cache = cache;
        _logger = logger;
    }

    public async Task Insert(Sale sale, CancellationToken cancellationToken = default)
    {
        if (sale == null)
        {
            throw new ArgumentNullException(nameof(sale));
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO sale (id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)", conn)
        {
            Parameters =
            {
                new NpgsqlParameter<Guid> { TypedValue = Guid.NewGuid() },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.WorldId) },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.ItemId) },
                new NpgsqlParameter<bool> { TypedValue = sale.Hq },
                new NpgsqlParameter<long> { TypedValue = Convert.ToInt64(sale.PricePerUnit) },
                new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.Quantity) },
                new NpgsqlParameter<string> { TypedValue = sale.BuyerName },
                new NpgsqlParameter<DateTime> { TypedValue = sale.SaleTime },
                new NpgsqlParameter<string> { TypedValue = sale.UploaderIdHash },
                new NpgsqlParameter<bool> { TypedValue = sale.OnMannequin ?? false },
            },
        };
        await command.ExecuteNonQueryAsync(cancellationToken);

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

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var batch = new NpgsqlBatch(conn);
        var purgedCaches = new Dictionary<string, bool>();
        foreach (var sale in sales)
        {
            // Purge the cache
            var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
            var cacheIndexKey = GetIndexCacheKey(sale.WorldId, sale.ItemId);
            if (!purgedCaches.TryGetValue(cacheIndexKey, out var purged) || !purged)
            {
                await cache.KeyDeleteAsync(cacheIndexKey, CommandFlags.FireAndForget);
                purgedCaches[cacheIndexKey] = true;
            }

            batch.BatchCommands.Add(new NpgsqlBatchCommand(
                "INSERT INTO sale (id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)")
            {
                Parameters =
                {
                    new NpgsqlParameter<Guid> { TypedValue = Guid.NewGuid() },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.WorldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.ItemId) },
                    new NpgsqlParameter<bool> { TypedValue = sale.Hq },
                    new NpgsqlParameter<long> { TypedValue = Convert.ToInt64(sale.PricePerUnit) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(sale.Quantity) },
                    new NpgsqlParameter<string> { TypedValue = sale.BuyerName },
                    new NpgsqlParameter<DateTime> { TypedValue = sale.SaleTime },
                    new NpgsqlParameter<string> { TypedValue = sale.UploaderIdHash },
                    new NpgsqlParameter<bool> { TypedValue = sale.OnMannequin ?? false },
                },
            });
        }
        await batch.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<Sale>> RetrieveBySaleTime(uint worldId, uint itemId, int count, DateTime? from = null, CancellationToken cancellationToken = default)
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

        // Fetch data from the database
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var command =
            new NpgsqlCommand(
                "SELECT id, world_id, item_id, hq, unit_price, quantity, buyer_name, sale_time, uploader_id, mannequin FROM sale WHERE world_id = $1 AND item_id = $2 AND sale_time <= $3 ORDER BY sale_time DESC LIMIT $4", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(worldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(itemId) },
                    new NpgsqlParameter<DateTime> { TypedValue = from ?? DateTime.UtcNow },
                    new NpgsqlParameter<int> { TypedValue = count + 20 }, // Give some buffer in case we filter out anything 
                },
            };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var sales = new List<Sale>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var nextSale = new Sale
            {
                Id = reader.GetGuid(0),
                WorldId = Convert.ToUInt32(reader.GetInt32(1)),
                ItemId = Convert.ToUInt32(reader.GetInt32(2)),
                Hq = reader.GetBoolean(3),
                PricePerUnit = Convert.ToUInt32(reader.GetInt64(4)),
                Quantity = reader.IsDBNull(5) ? null : Convert.ToUInt32(reader.GetInt32(5)),
                BuyerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                SaleTime = (DateTime)reader.GetValue(7),
                UploaderIdHash = reader.IsDBNull(8) ? null : reader.GetString(8),
                OnMannequin = reader.IsDBNull(9) ? null : reader.GetBoolean(9),
            };

            if (sales.Contains(nextSale))
            {
                continue;
            }

            sales.Add(nextSale);
        }

        var results = sales.Take(count).ToList();

        // Store the results in the cache
        if (results.Count >= MaxCachedSales)
        {
            await CacheSales(cache, cacheIndexKey, results);
        }

        return results;
    }

    public async Task<long> RetrieveUnitTradeVolume(uint worldId, uint itemId, DateTime from, CancellationToken cancellationToken = default)
    {
        // Check if the data needed is cached
        var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
        var cacheKey = GetTradeVolumeCacheKey(worldId, itemId);
        try
        {
            var cachedToRaw = await cache.HashGetAsync(cacheKey, "cached-to");
            if (DateTime.TryParse(cachedToRaw, out var cachedTo) && cachedTo <= from)
            {
                var saleVolumes = await cache.HashGetAllAsync(cacheKey, flags: CommandFlags.PreferReplica);
                return saleVolumes
                    .Where(e => DateTime.TryParse(e.Name, out _))
                    .Select(e => new KeyValuePair<DateTime, long>(DateTime.Parse(e.Name), (long)e.Value))
                    .Where(kvp => kvp.Key >= from)
                    .Select(kvp => kvp.Value)
                    .Sum();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{TradeVolumeCacheKey}\"", cacheKey);
        }

        // Request the sale velocity for the allowed intervals
        var result = await GetDailyUnitsTraded(worldId, itemId, from, cancellationToken);

        // Cache it
        try
        {
            var hash = result.Select(kvp => new HashEntry(kvp.Key.ToString(), kvp.Value)).ToList();
            hash.Add(new HashEntry("cached-to", from.ToString()));
            await cache.HashSetAsync(cacheKey, hash.ToArray(), CommandFlags.FireAndForget);
            await cache.KeyExpireAsync(cacheKey, TimeSpan.FromHours(1), CommandFlags.FireAndForget);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to store unit trade volumes \"{TradeVolumeCacheKey}\" in cache", cacheKey);
        }

        return result.Select(e => e.Value).Sum();
    }

    private async Task<IDictionary<DateTime, long>> GetDailyUnitsTraded(uint worldId, uint itemId, DateTime from, CancellationToken cancellationToken = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        // This is a relatively slow query that often causes cache misses.
        // I don't think anything can be done about this besides using more memory.
        await using var command =
            new NpgsqlCommand(
                "SELECT SUM(quantity), sale_time::date AS sale_date FROM sale WHERE world_id = $1 AND item_id = $2 AND sale_time > $3 GROUP BY sale_date", conn)
            {
                Parameters =
                {
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(worldId) },
                    new NpgsqlParameter<int> { TypedValue = Convert.ToInt32(itemId) },
                    new NpgsqlParameter<DateTime> { TypedValue = from },
                },
            };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var sales = new Dictionary<DateTime, long>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var quantity = reader.GetInt64(0);
            var date = (DateTime)reader.GetValue(1);
            sales.Add(date, quantity);
        }

        return sales;
    }

    private async Task<IList<Sale>> FetchSalesFromCache(IDatabase cache, string cacheIndexKey, uint worldId, uint itemId, int count)
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

    private Sale ParseSaleFromHash(Guid saleId, uint worldId, uint itemId, IDictionary<RedisValue, RedisValue> hash)
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
            PricePerUnit = GetValueUInt32(hash, "ppu"),
            Quantity = GetValueUInt32(hash, "q"),
            BuyerName = GetValueString(hash, "bn"),
            SaleTime = saleTime,
            UploaderIdHash = GetValueString(hash, "uid"),
            OnMannequin = GetValueBool(hash, "mann"),
        };
    }

    private static uint GetValueUInt32(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? (uint)hash[key] : 0;
    }

    private static bool GetValueBool(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) && (bool)hash[key];
    }

    private static string GetValueString(IDictionary<RedisValue, RedisValue> hash, string key)
    {
        return hash.ContainsKey(key) ? hash[key] : "";
    }

    private static string GetTradeVolumeCacheKey(uint worldId, uint itemId)
    {
        return $"sale-volume:{worldId}:{itemId}";
    }

    private static string GetIndexCacheKey(uint worldId, uint itemId)
    {
        return $"sale-index:{worldId}:{itemId}";
    }

    private static string GetSaleCacheKey(Guid saleId)
    {
        return $"sale:{saleId}";
    }
}