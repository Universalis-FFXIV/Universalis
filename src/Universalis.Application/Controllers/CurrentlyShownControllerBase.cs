using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Common;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

public class CurrentlyShownControllerBase : WorldDcControllerBase
{
    protected readonly ICurrentlyShownDbAccess CurrentlyShown;
    protected readonly ICache<CurrentlyShownQuery, MinimizedCurrentlyShownData> Cache;

    private static readonly Counter CacheHits = Metrics.CreateCounter("universalis_cache_hits", "Cache Hits");
    private static readonly Counter CacheMisses = Metrics.CreateCounter("universalis_cache_misses", "Cache Misses");
    private static readonly Gauge CacheEntries = Metrics.CreateGauge("universalis_cache_entries", "Cache Entries");

    public CurrentlyShownControllerBase(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb, ICache<CurrentlyShownQuery, MinimizedCurrentlyShownData> cache) : base(gameData)
    {
        CurrentlyShown = currentlyShownDb;
        Cache = cache;
    }

    protected async Task<MinimizedCurrentlyShownData> GetCurrentlyShownDataSingle(
        uint worldId,
        uint itemId,
        CancellationToken cancellationToken = default)
    {
        // Fetch data from the cache
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var cached = Cache.Get(new CurrentlyShownQuery { ItemId = itemId, WorldId = worldId });
        stopwatch.Stop();
        if (cached != null)
        {
            CacheHitMs.Observe(stopwatch.ElapsedMilliseconds);
            CacheHits.Inc();

            return cached;
        }

            CacheMisses.Inc();
        CacheMissMs.Observe(stopwatch.ElapsedMilliseconds);
        CacheMisses.Inc();

        // Retrieve data from the database
        var data = await CurrentlyShown.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);

        if (data == null)
        {
            return null;
        }

        // Transform data into a view
        var dataListings = await (data.Listings ?? new List<Listing>())
            .ToAsyncEnumerable()
            .SelectAwait(async l => await Util.ListingToView(l, cancellationToken))
            .ToListAsync(cancellationToken);

        var dataHistory = (data.RecentHistory ?? new List<Sale>())
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .Where(s => s.TimestampUnixSeconds > 0)
            .Select(s => new SaleView
            {
                Hq = s.Hq,
                PricePerUnit = s.PricePerUnit,
                Quantity = s.Quantity,
                Total = s.PricePerUnit * s.Quantity,
                TimestampUnixSeconds = (long)s.TimestampUnixSeconds,
                BuyerName = s.BuyerName,
                WorldId = worldId,
            })
            .ToList();

        var dataView = new MinimizedCurrentlyShownData
        {
            ItemId = itemId,
            WorldId = worldId,
            LastUploadTimeUnixMilliseconds = (long)data.LastUploadTimeUnixMilliseconds,
            Listings = dataListings,
            RecentHistory = dataHistory,
        };

        Cache.Set(new CurrentlyShownQuery { ItemId = itemId, WorldId = worldId }, dataView);
        CacheEntries.Set(Cache.Count);

        return dataView;
    }

    protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(
        WorldDc worldDc,
        uint[] worldIds,
        uint itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool noGst = false,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        CancellationToken cancellationToken = default)
    {
        var data = await worldIds
            .ToAsyncEnumerable()
            .SelectAwait(async worldId => await GetCurrentlyShownDataSingle(worldId, itemId, cancellationToken))
            .Where(o => o != null)
            .ToListAsync(cancellationToken);
        var resolved = data.Count > 0;

        var worlds = GameData.AvailableWorlds();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;
        var (worldUploadTimes, currentlyShown) = data
            .Aggregate(
                (EmptyWorldDictionary<Dictionary<uint, long>, long>(worldIds),
                    new MinimizedCurrentlyShownData { Listings = new List<ListingView>(), RecentHistory = new List<SaleView>() }),
                (agg, next) =>
                {
                    var (aggWorldUploadTimes, aggData) = agg;

                    // Convert database entities into views. Separate classes are used for the entities
                    // and the views in order to avoid any undesirable data leaking out into the public
                    // API through inheritance and to allow separate purposes for the properties to be
                    // described in the property names (e.g. CreatorIdHash in the view and CreatorId in
                    // the database entity).

                    aggData.Listings = next.Listings
                        .Select(l =>
                        {
                            if (!noGst)
                            {
                                l.PricePerUnit = (uint)Math.Ceiling(l.PricePerUnit * 1.05);
                            }

                            l.WorldId = worldDc.IsDc ? next.WorldId : null;
                            l.WorldName = worldDc.IsDc ? worlds[next.WorldId] : null;
                            return l;
                        })
                        .Concat(aggData.Listings)
                        .ToList();

                    aggData.RecentHistory = next.RecentHistory
                        .Where(s => entriesWithin < 0 || nowSeconds - s.TimestampUnixSeconds < entriesWithin)
                        .Select(s =>
                        {
                            s.WorldId = worldDc.IsDc ? next.WorldId : null;
                            s.WorldName = worldDc.IsDc ? worlds[next.WorldId] : null;
                            return s;
                        })
                        .Concat(aggData.RecentHistory)
                        .ToList();

                    aggData.LastUploadTimeUnixMilliseconds = Math.Max(next.LastUploadTimeUnixMilliseconds, aggData.LastUploadTimeUnixMilliseconds);

                    return (aggWorldUploadTimes, aggData);
                });

        currentlyShown.Listings.Sort((a, b) => (int)a.PricePerUnit - (int)b.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

        var nqListings = currentlyShown.Listings.Where(l => !l.Hq).ToList();
        var hqListings = currentlyShown.Listings.Where(l => l.Hq).ToList();
        var nqSales = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
        var hqSales = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();

        var view = new CurrentlyShownView
        {
            Listings = currentlyShown.Listings.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nListings).ToList(),
            RecentHistory = currentlyShown.RecentHistory.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nEntries).ToList(),
            ItemId = itemId,
            WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
            WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
            DcName = worldDc.IsDc ? worldDc.DcName : null,
            LastUploadTimeUnixMilliseconds = currentlyShown.LastUploadTimeUnixMilliseconds,
            StackSizeHistogram = new SortedDictionary<int, int>(GetListingsDistribution(currentlyShown.Listings)),
            StackSizeHistogramNq = new SortedDictionary<int, int>(GetListingsDistribution(nqListings)),
            StackSizeHistogramHq = new SortedDictionary<int, int>(GetListingsDistribution(hqListings)),
            SaleVelocity = GetSaleVelocity(currentlyShown.RecentHistory, now, statsWithin),
            SaleVelocityNq = GetSaleVelocity(nqSales, now, statsWithin),
            SaleVelocityHq = GetSaleVelocity(hqSales, now, statsWithin),
            CurrentAveragePrice = GetAveragePricePerUnit(currentlyShown.Listings),
            CurrentAveragePriceNq = GetAveragePricePerUnit(nqListings),
            CurrentAveragePriceHq = GetAveragePricePerUnit(hqListings),
            MinPrice = GetMinPricePerUnit(currentlyShown.Listings),
            MinPriceNq = GetMinPricePerUnit(nqListings),
            MinPriceHq = GetMinPricePerUnit(hqListings),
            MaxPrice = GetMaxPricePerUnit(currentlyShown.Listings),
            MaxPriceNq = GetMaxPricePerUnit(nqListings),
            MaxPriceHq = GetMaxPricePerUnit(hqListings),
            AveragePrice = GetAveragePricePerUnit(currentlyShown.RecentHistory),
            AveragePriceNq = GetAveragePricePerUnit(nqSales),
            AveragePriceHq = GetAveragePricePerUnit(hqSales),
            WorldUploadTimes = worldDc.IsWorld ? null : worldUploadTimes,
        };

        return (resolved, view);
    }

    private static TDictionary EmptyWorldDictionary<TDictionary, T>(IEnumerable<uint> worldIds) where TDictionary : IDictionary<uint, T>
    {
        var dict = (TDictionary)Activator.CreateInstance(typeof(TDictionary));
        foreach (var worldId in worldIds)
        {
            // ReSharper disable once PossibleNullReferenceException
            dict[worldId] = default;
        }

        return dict;
    }

    private static uint GetMinPricePerUnit<TPriceable>(IList<TPriceable> items) where TPriceable : IPriceable
    {
        return !items.Any() ? 0 : items.Select(s => s.PricePerUnit).Min();
    }

    private static uint GetMaxPricePerUnit<TPriceable>(IList<TPriceable> items) where TPriceable : IPriceable
    {
        return !items.Any() ? 0 : items.Select(s => s.PricePerUnit).Max();
    }

    private static float GetAveragePricePerUnit<TPriceable>(IList<TPriceable> items) where TPriceable : IPriceable
    {
        if (!items.Any())
        {
            return 0;
        }

        return Filters.RemoveOutliers(items.Select(s => (float)s.PricePerUnit), 3).Average();
    }

    private static float GetSaleVelocity(IEnumerable<SaleView> sales, long unixNowMs, long statsWithinMs)
    {
        return Statistics.VelocityPerDay(sales
            .Select(s => s.TimestampUnixSeconds * 1000), unixNowMs, statsWithinMs);
    }

    private static IDictionary<int, int> GetListingsDistribution(IEnumerable<ListingView> listings)
    {
        return Statistics.GetDistribution(listings
            .Select(s => s.Quantity)
            .Select(q => (int)q));
    }
}