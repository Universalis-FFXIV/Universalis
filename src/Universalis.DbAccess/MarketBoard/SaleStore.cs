using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStore : ISaleStore, IDisposable
{
    private readonly ICacheRedisMultiplexer _cache;
    private readonly ILogger<SaleStore> _logger;

    private readonly Lazy<ISession> _scylla;
    private readonly Lazy<IMapper> _mapper;
    private readonly Lazy<PreparedStatement> _insertStatement;

    private readonly SemaphoreSlim _lock;

    public SaleStore(ICluster scylla, ICacheRedisMultiplexer cache, ILogger<SaleStore> logger)
    {
        _cache = cache;
        _logger = logger;

        _lock = new SemaphoreSlim(500, 500);

        // Doing database initialization in a constructor is a Bad Idea and
        // can lead to timeouts killing the application, so this just gets
        // stuffed in a lazy loader for later.
        _scylla = new Lazy<ISession>(() =>
        {
            var db = scylla.Connect();
            db.CreateKeyspaceIfNotExists("sale");
            db.ChangeKeyspace("sale");
            var table = db.GetTable<Sale>();
            table.CreateIfNotExists();
            return db;
        });

        _insertStatement = new Lazy<PreparedStatement>(() => _scylla.Value.Prepare("" +
            "INSERT INTO sale" +
            "(id, sale_time, item_id, world_id, buyer_name, hq, on_mannequin, quantity, unit_price, uploader_id)" +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)"));
        _mapper = new Lazy<IMapper>(() => new Mapper(_scylla.Value));
    }

    public async Task Insert(Sale sale, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.Insert");

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
            await _mapper.Value.InsertAsync(sale);
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
        using var activity = Util.ActivitySource.StartActivity("SaleStore.InsertMany");

        if (sales == null)
        {
            throw new ArgumentNullException(nameof(sales));
        }

        // NOTE: the Tuple should match the PartitionKey for the sales table.
        var groupedSales = sales.GroupBy(sale => Tuple.Create(sale.ItemId, sale.WorldId));

        foreach (var groupSale in groupedSales)
        {
            var (itemId, worldId) = groupSale.Key; // this must match the tuple order.
            var batch = new BatchStatement();
            foreach (var sale in groupSale)
            {
                var bound = _insertStatement.Value.Bind(
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
            }

            try
            {
                await _scylla.Value.ExecuteAsync(batch);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to insert sales for itemID: {} on worldID: {}", itemId, worldId);
                throw;
            }

            // Purge the cache
            var cache = _cache.GetDatabase(RedisDatabases.Cache.Sales);
            var cacheIndexKey = GetIndexCacheKey(worldId, itemId);
            await cache.KeyDeleteAsync(cacheIndexKey, CommandFlags.FireAndForget);
        }
    }

    public async Task<IEnumerable<Sale>> RetrieveBySaleTime(int worldId, int itemId, int count, DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.RetrieveBySaleTime");

        // Reads from the sale table are prone to timeouts for some reason, so we throttle them here
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await RetrieveBySaleTimeCore(worldId, itemId, count, from);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IEnumerable<Sale>> RetrieveBySaleTimeCore(int worldId, int itemId, int count, DateTime? from)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.RetrieveBySaleTimeCore");
        activity?.AddTag("query.worldId", worldId);
        activity?.AddTag("query.itemId", itemId);
        activity?.AddTag("query.count", count);
        activity?.AddTag("query.from", from?.ToString("s", CultureInfo.InvariantCulture));

        if (count == 0)
        {
            return Enumerable.Empty<Sale>();
        }

        // Fetch data from the database
        var timestamp = from == null ? 0 : new DateTimeOffset(from.Value).ToUnixTimeMilliseconds();
        try
        {
            var sales = await _mapper.Value.FetchAsync<Sale>(
                "SELECT id, sale_time, item_id, world_id, buyer_name, hq, on_mannequin, quantity, unit_price, uploader_id FROM sale WHERE item_id=? AND world_id=? AND sale_time>=? ORDER BY sale_time DESC LIMIT ?",
                itemId, worldId, timestamp, count);
            return sales
                .Select(sale =>
                {
                    sale.SaleTime = DateTime.SpecifyKind(sale.SaleTime, DateTimeKind.Utc);
                    return sale;
                });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve sales (world={WorldId}, item={ItemId})", worldId, itemId);
            throw;
        }
    }

    public async Task<long> RetrieveUnitTradeVolume(int worldId, int itemId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.RetrieveUnitTradeVolume");

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
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{TradeVolumeCacheKey}\"",
                cacheKey);
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

    public async Task<long> RetrieveGilTradeVolume(int worldId, int itemId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.RetrieveGilTradeVolume");

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
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{TradeVolumeCacheKey}\"",
                cacheKey);
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

    private static async Task CacheTradeVolume(IDatabaseAsync cache, string cacheKey,
        IDictionary<DateTime, int> hashData,
        DateTime from, DateTime to)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.CacheTradeVolume");

        var hash = hashData.Select(kvp => new HashEntry(kvp.Key.ToString(CultureInfo.InvariantCulture), kvp.Value))
            .ToList();
        hash.Add(new HashEntry("cached-from", from.ToString(CultureInfo.InvariantCulture)));
        hash.Add(new HashEntry("cached-to", to.ToString(CultureInfo.InvariantCulture)));
        await cache.HashSetAsync(cacheKey, hash.ToArray(), CommandFlags.FireAndForget);
        await cache.KeyExpireAsync(cacheKey, TimeSpan.FromHours(1), CommandFlags.FireAndForget);
    }

    private Task<IDictionary<DateTime, int>> GetDailyUnitsTraded(int worldId, int itemId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla
        return Task.FromResult<IDictionary<DateTime, int>>(new Dictionary<DateTime, int>());
    }

    private Task<IDictionary<DateTime, int>> GetDailyGilTraded(int worldId, int itemId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        // TODO: Make this work with Scylla
        return Task.FromResult<IDictionary<DateTime, int>>(new Dictionary<DateTime, int>());
    }

    private static long AggregateHashVolume(IEnumerable<HashEntry> hash, DateTime from, DateTime to)
    {
        return hash
            .Where(e => DateTime.TryParse(e.Name, out _))
            .Select(e => new KeyValuePair<DateTime, long>(DateTime.Parse(e.Name), (long)e.Value))
            .Where(kvp => kvp.Key >= from)
            .Where(kvp => kvp.Key <= to)
            .Select(kvp => kvp.Value)
            .Sum();
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

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}