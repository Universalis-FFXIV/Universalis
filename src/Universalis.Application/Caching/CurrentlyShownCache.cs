using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;

namespace Universalis.Application.Caching;

public class CurrentlyShownCache : MemoryCache<CurrentlyShownQuery, CachedCurrentlyShownData>
{
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IHistoryDbAccess _historyDb;
    
    private static readonly Counter CacheHits = Metrics.CreateCounter("universalis_cache_hits", "Cache Hits");
    private static readonly Counter CacheMisses = Metrics.CreateCounter("universalis_cache_misses", "Cache Misses");
    private static readonly Gauge CacheEntries = Metrics.CreateGauge("universalis_cache_entries", "Cache Entries");
    private static readonly Histogram CacheHitMs = Metrics.CreateHistogram("universalis_cache_hit_milliseconds", "Cache Hit Milliseconds");
    private static readonly Histogram CacheMissMs = Metrics.CreateHistogram("universalis_cache_miss_milliseconds", "Cache Miss Milliseconds");
    private static readonly Counter CacheDeletes = Metrics.CreateCounter("universalis_cache_deletes", "Cache Deletes");
    private static readonly Counter CacheEvictions = Metrics.CreateCounter("universalis_cache_evictions", "Cache Evictions");
    
    public CurrentlyShownCache(int size, ICurrentlyShownDbAccess currentlyShownDb, IHistoryDbAccess historyDb) : base(size)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
    }

    public override async Task<CachedCurrentlyShownData> Get(CurrentlyShownQuery key, CancellationToken cancellationToken = default)
    {
        // Fetch data from the cache
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var cached = await base.Get(key, cancellationToken);
        if (cached != null)
        {
            stopwatch.Stop();
            CacheHitMs.Observe(stopwatch.ElapsedMilliseconds);
            CacheHits.Inc();

            return cached;
        }

        // Retrieve data from the database
        var currentData = await _currentlyShownDb.Retrieve(key, cancellationToken);
        var history = await _historyDb.Retrieve(new HistoryQuery { WorldId = key.WorldId, ItemId = key.ItemId, Count = 20 }, cancellationToken);

        stopwatch.Stop();
        CacheMissMs.Observe(stopwatch.ElapsedMilliseconds);
        CacheMisses.Inc();

        if (currentData == null || history == null)
        {
            return null;
        }

        // Transform data into a view
        var dataConversions = await Task.WhenAll((currentData.Listings ?? new List<Listing>())
            .Select(l => Util.ListingSimpleToView(l, cancellationToken)));
        var dataListings = dataConversions
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .ToList();

        var dataHistory = (history.Sales ?? new List<Sale>())
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .Where(s => s.SaleTime.ToUnixTimeSeconds() > 0)
            .Select(Util.SaleSimpleToView)
            .ToList();

        var dataView = new CachedCurrentlyShownData
        {
            ItemId = key.ItemId,
            WorldId = key.WorldId,
            LastUploadTimeUnixMilliseconds = Convert.ToInt64(history.LastUploadTimeUnixMilliseconds),
            Listings = dataListings,
            RecentHistory = dataHistory,
        };

        await Set(key, dataView, cancellationToken);

        return dataView;
    }

    public override async Task Set(CurrentlyShownQuery key, CachedCurrentlyShownData value, CancellationToken cancellationToken = default)
    {
        await base.Set(key, value, cancellationToken);
        CacheEntries.Set(Count);
    }

    public override async Task<bool> Delete(CurrentlyShownQuery key, CancellationToken cancellationToken = default)
    {
        var result = await base.Delete(key, cancellationToken);
        if (result)
        {
            CacheDeletes.Inc();
        }

        return result;
    }

    protected override bool Evict()
    {
        var result = base.Evict();
        if (result)
        {
            CacheEvictions.Inc();
        }

        return result;
    }
}