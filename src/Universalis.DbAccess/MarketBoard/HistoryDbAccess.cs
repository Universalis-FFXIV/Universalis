using Enyim.Caching.Memcached;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class HistoryDbAccess : IHistoryDbAccess
{
    private readonly IMarketItemStore _marketItemStore;
    private readonly ISaleStore _saleStore;

    // This cache is in the service layer because it needs to be accessed conditionally.
    // There should be a separate service with a cache, eventually.
    private readonly IMemcachedCluster _memcached;
    private readonly ILogger<HistoryDbAccess> _logger;

    public HistoryDbAccess(IMarketItemStore marketItemStore, ISaleStore saleStore, IMemcachedCluster memcached, ILogger<HistoryDbAccess> logger)
    {
        _marketItemStore = marketItemStore;
        _saleStore = saleStore;
        _memcached = memcached;
        _logger = logger;
    }

    public async Task Create(History document, CancellationToken cancellationToken = default)
    {
        await _marketItemStore.Insert(new MarketItem
        {
            WorldId = document.WorldId,
            ItemId = document.ItemId,
            LastUploadTime =
                DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(document.LastUploadTimeUnixMilliseconds)).UtcDateTime,
        }, cancellationToken);
        await _saleStore.InsertMany(document.Sales, cancellationToken);
    }

    public async Task<History> Retrieve(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        var marketItem = await _marketItemStore.Retrieve(query.WorldId, query.ItemId, cancellationToken);
        if (marketItem == null)
        {
            return null;
        }
        
        var sales = await _saleStore.RetrieveBySaleTime(query.WorldId, query.ItemId, query.Count ?? 1000, cancellationToken: cancellationToken);
        return new History
        {
            WorldId = marketItem.WorldId,
            ItemId = marketItem.ItemId,
            LastUploadTimeUnixMilliseconds = new DateTimeOffset(marketItem.LastUploadTime).ToUnixTimeMilliseconds(),
            Sales = sales.ToList(),
        };
    }

    public async Task<History> RetrieveWithCache(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        var cache = _memcached.GetClient();
        var cacheKey = $"history:{query.WorldId}:{query.ItemId}:{query.Count}";
        var cacheData1 = await cache.GetWithResultAsync<string>(cacheKey);
        if (cacheData1.Success)
        {
            try
            {
                var cacheObject = JsonSerializer.Deserialize<History>(cacheData1.Value);
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

        var result = await Retrieve(query, cancellationToken);

        var cacheData2 = JsonSerializer.Serialize(result);
        await cache.SetAsync(cacheKey, cacheData2, Expiration.From(TimeSpan.FromSeconds(300)));

        return result;
    }

    public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query, CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(query.WorldIds
            .Select(worldId => Retrieve(new HistoryQuery { WorldId = worldId, ItemId = query.ItemId, Count = query.Count }, cancellationToken))))
            .Where(h => h != null);
    }

    public async Task InsertSales(IEnumerable<Sale> sales, HistoryQuery query, CancellationToken cancellationToken = default)
    {
        // Dirty hack that needs to be cleaned up later
        var cache = _memcached?.GetClient();
        var cacheKey = $"history:{query.WorldId}:{query.ItemId}:20";
        try
        {
            if (cache != null)
            {
                await cache.DeleteAsync(cacheKey);
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to delete object with key \"{CacheKey}\"", cacheKey);
        }

        await _marketItemStore.Update(new MarketItem
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTime = DateTime.UtcNow,
        }, cancellationToken);
        await _saleStore.InsertMany(sales, cancellationToken);
    }
}