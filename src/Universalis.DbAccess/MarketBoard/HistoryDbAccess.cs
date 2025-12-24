using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public class HistoryDbAccess : IHistoryDbAccess
{
    private readonly IMarketItemStore _marketItemStore;
    private readonly ISaleStore _saleStore;
    private readonly int _batchSize;

    public HistoryDbAccess(IMarketItemStore marketItemStore, ISaleStore saleStore)
    {
        _marketItemStore = marketItemStore;
        _saleStore = saleStore;

        // Configure batch size from environment variable (default: 50)
        var batchSizeStr = Environment.GetEnvironmentVariable("UNIVERSALIS_SALES_BATCH_SIZE") ?? "50";
        if (int.TryParse(batchSizeStr, out var batchSize) && batchSize > 0)
        {
            _batchSize = batchSize;
        }
        else
        {
            _batchSize = 50; // Default fallback
        }
    }

    public async Task Create(History document, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("HistoryDbAccess.Create");

        await _marketItemStore.Insert(new MarketItem
        {
            WorldId = document.WorldId,
            ItemId = document.ItemId,
            LastUploadTime =
                DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(document.LastUploadTimeUnixMilliseconds))
                    .UtcDateTime,
        }, cancellationToken);
        await _saleStore.InsertMany(document.Sales, cancellationToken);
    }

    public async Task<History> Retrieve(HistoryQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Count == 0)
        {
            return null;
        }

        using var activity = Util.ActivitySource.StartActivity("HistoryDbAccess.Retrieve");

        var marketItem =
            await _marketItemStore.Retrieve(new MarketItemQuery { ItemId = query.ItemId, WorldId = query.WorldId },
                cancellationToken);
        if (marketItem == null)
        {
            return null;
        }

        var sales = await _saleStore.RetrieveBySaleTime(query.WorldId, query.ItemId, query.Count ?? 1000,
            cancellationToken: cancellationToken);
        return new History
        {
            WorldId = marketItem.WorldId,
            ItemId = marketItem.ItemId,
            LastUploadTimeUnixMilliseconds = new DateTimeOffset(marketItem.LastUploadTime).ToUnixTimeMilliseconds(),
            Sales = sales.ToList(),
        };
    }

    public async Task<IEnumerable<History>> RetrieveMany(HistoryManyQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Count == 0)
        {
            return Enumerable.Empty<History>();
        }

        using var activity = Util.ActivitySource.StartActivity("HistoryDbAccess.RetrieveMany");

        // Build tuples of world/item pairs - the awkward syntax here avoids allocations besides the ToArray calls
        var worldIds = query.WorldIds.ToArray();
        var itemIds = query.ItemIds.ToArray();
        var worldItemTuples = Enumerable.Repeat(itemIds, worldIds.Length)
            .Zip(worldIds)
            .SelectMany(static tup =>
            {
                var (iIds, worldId) = tup;
                return Enumerable.Repeat(worldId, iIds.Length).Zip(iIds);
            })
            .ToArray();

        // Get upload times
        var marketItems =
            await _marketItemStore.RetrieveMany(
                new MarketItemManyQuery { ItemIds = query.ItemIds, WorldIds = query.WorldIds },
                cancellationToken);
        var marketItemsList = marketItems.ToList();
        var marketItemsDict = marketItemsList.ToDictionary(mi => (mi.WorldId, mi.ItemId), mi => mi);

        // Get sales where an upload time is known
        // Process in batches to avoid overwhelming the Scylla coordinator while still being faster than sequential
        var sales = new Dictionary<(int, int), IEnumerable<Sale>>();
        var filteredTuples = worldItemTuples.Where(marketItemsDict.ContainsKey).ToArray();

        for (var i = 0; i < filteredTuples.Length; i += _batchSize)
        {
            var batch = filteredTuples.Skip(i).Take(_batchSize);

            var tasks = batch.Select(async tuple =>
            {
                var (worldId, itemId) = tuple;
                var salesData = await _saleStore.RetrieveBySaleTime(worldId, itemId, query.Count ?? 200, query.From, query.To,
                    cancellationToken: cancellationToken);
                return (Key: (worldId, itemId), Value: salesData);
            });

            var results = await Task.WhenAll(tasks);
            foreach (var (key, value) in results)
            {
                sales[key] = value;
            }
        }

        // Reformat the results as a History instance
        return marketItemsList
            .Select(mi => (mi, sales[(mi.WorldId, mi.ItemId)]))
            .AsParallel()
            .Select(tup =>
            {
                var (mi, marketSales) = tup;
                return new History
                {
                    WorldId = mi.WorldId,
                    ItemId = mi.ItemId,
                    LastUploadTimeUnixMilliseconds = new DateTimeOffset(mi.LastUploadTime).ToUnixTimeMilliseconds(),
                    Sales = marketSales
                        .AsParallel() // Iterating over the sales retrieved by the DataStax driver is synchronous, so we parallelize it
                        .ToList(),
                };
            });
    }

    public async Task InsertSales(ICollection<Sale> sales, HistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("HistoryDbAccess.InsertSales");

        await _marketItemStore.Insert(new MarketItem
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            LastUploadTime = DateTime.UtcNow,
        }, cancellationToken);
        await _saleStore.InsertMany(sales, cancellationToken);
    }
}