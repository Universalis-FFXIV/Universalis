using System;
using System.Collections.Generic;
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
    protected readonly IHistoryDbAccess History;
    protected readonly ICache<CurrentlyShownQuery, CachedCurrentlyShownData> Cache;

    public CurrentlyShownControllerBase(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb, IHistoryDbAccess history, ICache<CurrentlyShownQuery, CachedCurrentlyShownData> cache) : base(gameData)
    {
        CurrentlyShown = currentlyShownDb;
        History = history;
        Cache = cache;
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
        var fetches = worldIds
            .Select(async worldId =>
            {
                var q = new CurrentlyShownQuery { WorldId = worldId, ItemId = itemId };
                var d = await Cache.Get(q, cancellationToken);
                if (d == null)
                {
                    var cd = await CurrentlyShown.Retrieve(q, cancellationToken);
                    if (cd == null)
                    {
                        return null;
                    }

                    var h = await History.Retrieve(new HistoryQuery { WorldId = worldId, ItemId = itemId, Count = 20 }, cancellationToken);
                    
                    return new CachedCurrentlyShownData
                    {
                        WorldId = cd.WorldId,
                        ItemId = cd.ItemId,
                        LastUploadTimeUnixMilliseconds = cd.LastUploadTimeUnixMilliseconds,
                        Listings = (await Task.WhenAll((cd.Listings ?? new List<Listing>())
                                .Select(l => Util.ListingToView(l, cancellationToken))))
                            .Where(s => s.PricePerUnit > 0)
                            .Where(s => s.Quantity > 0)
                            .ToList(),
                        RecentHistory = (h?.Sales ?? new List<Sale>())
                            .Select(Util.SaleToView)
                            .Where(s => s.PricePerUnit > 0)
                            .Where(s => s.Quantity > 0)
                            .Where(s => s.TimestampUnixSeconds > 0)
                            .ToList(),
                    };
                }

                return d;
            });
        var data = (await Task.WhenAll(fetches))
            .Where(o => o != null)
            .ToList();
        var resolved = data.Count > 0;

        var worlds = GameData.AvailableWorlds();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;
        var (worldUploadTimes, currentlyShown) = data
            .Aggregate(
                (EmptyWorldDictionary<Dictionary<uint, long>, long>(worldIds),
                    new CachedCurrentlyShownData { Listings = new List<ListingView>(), RecentHistory = new List<SaleView>() }),
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
                                l.Total = (uint)Math.Ceiling(l.Total * 1.05);
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