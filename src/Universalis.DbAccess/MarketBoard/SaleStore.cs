using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using Prometheus;
using StackExchange.Redis;
using Universalis.Common.GameData;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class SaleStore : ISaleStore, IDisposable
{
    private static readonly Histogram RowsReadCount =
        Prometheus.Metrics.CreateHistogram("universalis_sale_rows_read", "", new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(1, 2, 16),
        });

    private readonly IPersistentRedisMultiplexer _cache;
    private readonly ILogger<SaleStore> _logger;

    private readonly Lazy<ISession> _scylla;
    private readonly Lazy<IMapper> _mapper;
    private readonly Lazy<PreparedStatement> _insertStatement;
    private readonly IWorldToDcRegion _worldToDcRegion;

    private readonly SemaphoreSlim _lock;

    public SaleStore(ICluster scylla, IPersistentRedisMultiplexer cache, ILogger<SaleStore> logger, IWorldToDcRegion worldToDcRegion)
    {
        _cache = cache;
        _logger = logger;
        _worldToDcRegion = worldToDcRegion;

        _lock = new SemaphoreSlim(1000, 1000);

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

    public async Task InsertMany(ICollection<Sale> sales, CancellationToken cancellationToken = default)
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
        }
        await WriteSaleCache(_cache.GetDatabase(RedisDatabases.Instance0.Aggregates), sales, cancellationToken);
    }

    private async Task WriteSaleCache(IDatabase cache, IEnumerable<Sale> sales, CancellationToken cancellationToken = default)
    {
        try
        {
            var expireTime = DateTime.Today.AddDays(7);
            foreach (var byWorld in sales.GroupBy(s => s.WorldId))
            {
                var worldId = byWorld.Key;
                var (dc, region) = _worldToDcRegion.Get(worldId);
                var scopes = new[] { worldId.ToString(), dc, region };
                foreach (var group in byWorld.GroupBy(s => (DateOnly.FromDateTime(s.SaleTime), s.ItemId, s.Hq)))
                {
                    var (date, itemId, hq) = group.Key;
                    var quantitySum = group.Sum(i => (long)(i.Quantity ?? 0));
                    var priceSum = group.Sum(i => (long)(i.Quantity ?? 0) * i.PricePerUnit);
                    // increment trade volume for world, datacenter and region
                    foreach (var scope in scopes)
                    {
                        var priceAggKey = GetTradeVolumeCacheKey(scope, itemId, hq, true, date);
                        await cache.StringIncrementAsync(priceAggKey, quantitySum, CommandFlags.FireAndForget);
                        await cache.KeyExpireAsync(priceAggKey, expireTime, ExpireWhen.HasNoExpiry, CommandFlags.FireAndForget);
                        var quantAggKey = GetTradeVolumeCacheKey(scope, itemId, hq, false, date);
                        await cache.StringIncrementAsync(quantAggKey, priceSum, CommandFlags.FireAndForget);
                        await cache.KeyExpireAsync(quantAggKey, expireTime, ExpireWhen.HasNoExpiry, CommandFlags.FireAndForget);
                    }
                }
                // write the timestamps of the most recent sale
                foreach (var ((itemId, hq), sale) in byWorld.GroupBy(s => (s.ItemId, s.Hq)).Select(g => (g.Key, g.MaxBy(s => s.SaleTime))))
                {
                    var time = new DateTimeOffset(sale.SaleTime).ToUnixTimeMilliseconds();
                    var worldKey = GetRecentSaleCacheKey(worldId.ToString(), itemId, hq);
                    var values = new KeyValuePair<RedisKey, RedisValue>[] { new($"{worldKey}:time", time), new($"{worldKey}:price", sale.PricePerUnit) };
                    await cache.StringSetAsync(values, flags: CommandFlags.FireAndForget);
                    await cache.SortedSetAddAsync(GetRecentSaleCacheKey(dc, itemId, hq), worldId, time, SortedSetWhen.GreaterThan, CommandFlags.FireAndForget);
                    await cache.SortedSetAddAsync(GetRecentSaleCacheKey(region, itemId, hq), worldId, time, SortedSetWhen.GreaterThan, CommandFlags.FireAndForget);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create sales cache");
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
            activity?.AddEvent(new ActivityEvent("CassandraFetchAsync"));
            RowsReadCount.Observe(count);
            var sales = await _mapper.Value.FetchAsync<Sale>(
                "SELECT id, sale_time, item_id, world_id, buyer_name, hq, on_mannequin, quantity, unit_price, uploader_id FROM sale WHERE item_id=? AND world_id=? AND sale_time>=? ORDER BY sale_time DESC LIMIT ?",
                itemId, worldId, timestamp, count);
            return sales
                .Select(static sale =>
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

    public async Task<(TradeVelocity Nq, TradeVelocity Hq)> RetrieveUnitTradeVelocity(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.RetrieveUnitTradeVelocity");

        // Check if the data needed is cached
        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var quantityNq = 0L;
        var quantityHq = 0L;
        var sumSalesNq = 0L;
        var sumSalesHq = 0L;
        var cacheKeys = GetUnitTradeVolumeCacheKeys(worldIdDcRegion, itemId, from, to).ToList();
        try
        {
            var cached = await cache.StringGetAsync(cacheKeys.Select(c => c.Key).ToArray(), CommandFlags.PreferReplica);

            for (var i = 0; i < cacheKeys.Count; i++)
            {
                if (cached[i].IsNull || !cached[i].TryParse(out long val)) continue;
                _ = (cacheKeys[i].Hq, cacheKeys[i].IsQuantity) switch
                {
                    (true, true) => quantityHq += val,
                    (true, false) => sumSalesHq += val,
                    (false, true) => quantityNq += val,
                    (false, false) => sumSalesNq += val,
                };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to retrieve cached unit trade volumes for key \"{WorldIdDcRegion}\" \"{ItemId}\"",
                worldIdDcRegion, itemId);
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startTime = from.ToDateTime(TimeOnly.MinValue);
        var endTime = to == today ? DateTime.UtcNow : to.ToDateTime(TimeOnly.MaxValue);
        var totalDays = (endTime - startTime).TotalDays;

        return (
            quantityNq > 0
                ? new TradeVelocity
                {
                    Quantity = quantityNq,
                    SumSales = sumSalesNq,
                    AvgSalesPerDay = quantityNq / totalDays,
                }
                : null,
            quantityHq > 0
                ? new TradeVelocity
                {
                    Quantity = quantityHq,
                    SumSales = sumSalesHq,
                    AvgSalesPerDay = quantityHq / totalDays,
                }
                : null
        );
    }

    private static IEnumerable<TradeVolumeCacheKey> GetUnitTradeVolumeCacheKeys(string worldIdDcRegion, int itemId, DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
            foreach (var isQuantity in new[] { false, true })
            foreach (var isHq in new[] { false, true })
                yield return new TradeVolumeCacheKey(isHq, isQuantity, GetTradeVolumeCacheKey(worldIdDcRegion, itemId, isHq, isQuantity, date));
    }

    public async Task<RecentSale> GetMostRecentSaleInWorld(int worldId, int itemId, bool hq, CancellationToken cancellationToken = default)
    {
        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var key = GetRecentSaleCacheKey(worldId.ToString(), itemId, hq);
        var sale = await cache.StringGetAsync(new RedisKey[] { $"{key}:time", $"{key}:price" });
        if (sale[0] != RedisValue.Null && sale[1] != RedisValue.Null && sale[0].TryParse(out long time) && sale[1].TryParse(out int price))
            return new RecentSale
            {
                UnitPrice = price,
                SaleTime = DateTimeOffset.FromUnixTimeMilliseconds(time).UtcDateTime,
                WorldId = worldId,
            };
        return null;
    }

    public async Task<RecentSale> GetMostRecentSaleInDatacenterOrRegion(string dcOrRegion, int itemId, bool hq, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SaleStore.GetMostRecentSaleInDatacenterOrRegion");

        var cache = _cache.GetDatabase(RedisDatabases.Instance0.Aggregates);
        var key = GetRecentSaleCacheKey(dcOrRegion, itemId, hq);
        var results = await cache.SortedSetRangeByScoreAsync(key, order: Order.Descending, take: 1, flags: CommandFlags.PreferReplica);
        if (results.Length == 0 || !results[0].TryParse(out int worldId))
            return null;
        return await GetMostRecentSaleInWorld(worldId, itemId, hq, cancellationToken);
    }

    private static RedisKey GetRecentSaleCacheKey(string dcOrRegion, int itemId, bool hq) =>
        $"recent-sales:{dcOrRegion}:{itemId}:{(hq ? "hq" : "nq")}";

    private static RedisKey GetTradeVolumeCacheKey(string worldIdDcRegion, int itemId, bool isHq, bool isQuantity, DateOnly date) =>
        $"sale-{(isQuantity ? "qu" : "pr")}:{worldIdDcRegion}:{itemId}:{(isHq ? "hq" : "nq")}:{date:O}";

    private record TradeVolumeCacheKey(bool Hq, bool IsQuantity, RedisKey Key);

    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
