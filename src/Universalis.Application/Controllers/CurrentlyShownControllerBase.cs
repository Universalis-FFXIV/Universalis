using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

public class CurrentlyShownControllerBase : WorldDcRegionControllerBase
{
    protected readonly ICurrentlyShownDbAccess CurrentlyShown;
    protected readonly IHistoryDbAccess History;

    public CurrentlyShownControllerBase(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess history) : base(gameData)
    {
        CurrentlyShown = currentlyShownDb;
        History = history;
    }

    protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool noGst = false,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        if (worldIds.Length == 0)
        {
            throw new InvalidOperationException("Must query at least one world.");
        }

        if (worldIds.Length == 1)
        {
            return await GetView(worldDcRegion, worldIds[0], itemId, nListings, nEntries, noGst, onlyHq, statsWithin, entriesWithin, fields, cancellationToken);
        }

        return await GetViewBatched(worldDcRegion, worldIds, itemId, nListings, nEntries, noGst, onlyHq, statsWithin, entriesWithin, fields, cancellationToken);
    }

    protected async Task<(bool, CurrentlyShownView)> GetView(
        WorldDcRegion worldDcRegion,
        int worldId,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool noGst = false,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.GetView");

        if (!GameData.MarketableItemIds().Contains(itemId))
        {
            return (false, new CurrentlyShownView
            {
                ItemId = itemId,
                WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
                WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
                DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
                RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
                SerializableProperties = BuildSerializableProperties(fields),
            });
        }

        var currentlyShown = await FetchData(worldId, itemId, cancellationToken);

        var listingSerializableProperties = BuildSerializableProperties(fields, "listings");
        var recentHistorySerializableProperties = BuildSerializableProperties(fields, "recentHistory");

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;
        var worlds = GameData.AvailableWorlds();

        if (currentlyShown.LastUploadTimeUnixMilliseconds == 0)
        {
            return (false, new CurrentlyShownView
            {
                ItemId = itemId,
                WorldId = worldId,
                WorldName = worlds[worldId],
                DcName = null,
                RegionName = null,
                SerializableProperties = BuildSerializableProperties(fields),
            });
        }

        currentlyShown.Listings = currentlyShown.Listings
            .Select(l =>
            {
                if (!noGst)
                {
                    l.PricePerUnit = (int)Math.Ceiling(l.PricePerUnit * 1.05);
                    l.Total = (int)Math.Ceiling(l.Total * 1.05);
                }

                l.WorldId = null;
                l.WorldName = null;
                l.SerializableProperties = listingSerializableProperties;
                return l;
            })
            .ToList();

        currentlyShown.RecentHistory = currentlyShown.RecentHistory
            .Where(s => entriesWithin < 0 || nowSeconds - s.TimestampUnixSeconds < entriesWithin)
            .Select(s =>
            {
                s.WorldId = null;
                s.WorldName = null;
                s.SerializableProperties = recentHistorySerializableProperties;
                return s;
            })
            .ToList();

        currentlyShown.Listings.Sort((a, b) => a.PricePerUnit - b.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

        var nqListings = currentlyShown.Listings.Where(l => !l.Hq).ToList();
        var hqListings = currentlyShown.Listings.Where(l => l.Hq).ToList();
        var nqSales = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
        var hqSales = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();

        var view = new CurrentlyShownView
        {
            Listings = currentlyShown.Listings.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nListings).ToList(),
            RecentHistory = currentlyShown.RecentHistory.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nEntries)
                .ToList(),
            ItemId = itemId,
            WorldId = worldId,
            WorldName = worlds[worldId],
            DcName = null,
            RegionName = null,
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
            WorldUploadTimes = null,
            SerializableProperties = BuildSerializableProperties(fields),
        };

        return (true, view);
    }

    protected async Task<(bool, CurrentlyShownView)> GetViewBatched(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool noGst = false,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.GetViewBatched");

        if (!GameData.MarketableItemIds().Contains(itemId))
        {
            return (false, new CurrentlyShownView
            {
                ItemId = itemId,
                WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
                WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
                DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
                RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
                SerializableProperties = BuildSerializableProperties(fields),
            });
        }

        var data = await FetchDataBatched(worldIds, new[] { itemId }, cancellationToken);

        var worlds = GameData.AvailableWorlds();

        var listingSerializableProperties = BuildSerializableProperties(fields, "listings");
        var recentHistorySerializableProperties = BuildSerializableProperties(fields, "recentHistory");

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;

        var (worldUploadTimes, currentlyShown) = data
            .Aggregate(
                (EmptyWorldDictionary<Dictionary<int, long>, long>(worldIds),
                    new CurrentlyShownView
                    { Listings = new List<ListingView>(), RecentHistory = new List<SaleView>() }),
                (agg, next) =>
                {
                    if (next.WorldId == null)
                    {
                        return agg;
                    }

                    var (aggWorldUploadTimes, aggData) = agg;

                    // Convert database entities into views. Separate classes are used for the entities
                    // and the views in order to avoid any undesirable data leaking out into the public
                    // API through inheritance and to allow separate purposes for the properties to be
                    // described in the property names (e.g. CreatorIdHash in the view and CreatorId in
                    // the database entity).

                    aggData.Listings.AddRange(next.Listings
                        .Select(l =>
                        {
                            if (!noGst)
                            {
                                l.PricePerUnit = (int)Math.Ceiling(l.PricePerUnit * 1.05);
                                l.Total = (int)Math.Ceiling(l.Total * 1.05);
                            }

                            l.WorldId = !worldDcRegion.IsWorld ? next.WorldId : null;
                            l.WorldName = !worldDcRegion.IsWorld ? worlds[next.WorldId.Value] : null;
                            l.SerializableProperties = listingSerializableProperties;
                            return l;
                        }));

                    aggData.RecentHistory.AddRange(next.RecentHistory
                        .Where(s => entriesWithin < 0 || nowSeconds - s.TimestampUnixSeconds < entriesWithin)
                        .Select(s =>
                        {
                            s.WorldId = !worldDcRegion.IsWorld ? next.WorldId : null;
                            s.WorldName = !worldDcRegion.IsWorld ? worlds[next.WorldId.Value] : null;
                            s.SerializableProperties = recentHistorySerializableProperties;
                            return s;
                        }));

                    aggData.LastUploadTimeUnixMilliseconds = Math.Max(next.LastUploadTimeUnixMilliseconds,
                        aggData.LastUploadTimeUnixMilliseconds);

                    aggWorldUploadTimes[next.WorldId.Value] = next.LastUploadTimeUnixMilliseconds;

                    return (aggWorldUploadTimes, aggData);
                });

        if (currentlyShown.LastUploadTimeUnixMilliseconds == 0)
        {
            return (false, new CurrentlyShownView
            {
                ItemId = itemId,
                WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
                WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
                DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
                RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
                SerializableProperties = BuildSerializableProperties(fields),
            });
        }

        currentlyShown.Listings.Sort((a, b) => a.PricePerUnit - b.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

        var nqListings = currentlyShown.Listings.Where(l => !l.Hq).ToList();
        var hqListings = currentlyShown.Listings.Where(l => l.Hq).ToList();
        var nqSales = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
        var hqSales = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();

        var view = new CurrentlyShownView
        {
            Listings = currentlyShown.Listings.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nListings).ToList(),
            RecentHistory = currentlyShown.RecentHistory.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nEntries)
                .ToList(),
            ItemId = itemId,
            WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
            WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
            DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
            RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
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
            WorldUploadTimes = worldDcRegion.IsWorld ? null : worldUploadTimes,
            SerializableProperties = BuildSerializableProperties(fields),
        };

        return (true, view);
    }

    private async Task<CurrentlyShownView> FetchData(int worldId, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.FetchData");

        var csTask = CurrentlyShown.Retrieve(new CurrentlyShownQuery { WorldId = worldId, ItemId = itemId },
            cancellationToken);
        var hTask = History.Retrieve(new HistoryQuery { WorldId = worldId, ItemId = itemId, Count = 20 },
            cancellationToken);
        await Task.WhenAll(csTask, hTask);

        var cs = await csTask;
        var history = await hTask;

        return BuildPartialView(cs ?? new CurrentlyShown(), history ?? new History());
    }

    private async Task<IEnumerable<CurrentlyShownView>> FetchDataBatched(IEnumerable<int> worlds, IEnumerable<int> items,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.FetchDataBatched");

        var worldIds = worlds.ToList();
        var itemIds = items.ToList();
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        var csTask = CurrentlyShown.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = worldIds, ItemIds = itemIds },
            cancellationToken);
        var hTask = History.RetrieveMany(new HistoryManyQuery { WorldIds = worldIds, ItemIds = itemIds, Count = 20 },
            cancellationToken);
        await Task.WhenAll(csTask, hTask);

        var cs = await csTask;
        var csDict = cs.ToDictionary(o => new WorldItemPair(o.WorldId, o.ItemId));
        var history = await hTask;
        var historyDict = history.ToDictionary(o => new WorldItemPair(o.WorldId, o.ItemId));

        return worldItemPairs
            .Select(wi => BuildPartialView(
                csDict.TryGetValue(new WorldItemPair(wi.WorldId, wi.ItemId), out var c) ? c : new CurrentlyShown(),
                historyDict.TryGetValue(new WorldItemPair(wi.WorldId, wi.ItemId), out var h) ? h : new History()));
    }

    private static CurrentlyShownView BuildPartialView(CurrentlyShown currentlyShown, History history)
    {
        var worldId = currentlyShown.WorldId == default ? history.WorldId : currentlyShown.WorldId;
        var itemId = currentlyShown.ItemId == default ? history.ItemId : currentlyShown.ItemId;
        var lastUploadTime = Math.Max(currentlyShown.LastUploadTimeUnixMilliseconds,
            Convert.ToInt64(history.LastUploadTimeUnixMilliseconds));
        return new CurrentlyShownView
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = lastUploadTime,
            Listings = (currentlyShown.Listings ?? Enumerable.Empty<Listing>())
                .Select(Util.ListingToView)
                .Where(s => s.PricePerUnit > 0)
                .Where(s => s.Quantity > 0)
                .ToList(),
            RecentHistory = (history.Sales ?? Enumerable.Empty<Sale>())
                .Select(Util.SaleToView)
                .Where(s => s.PricePerUnit > 0)
                .Where(s => s.Quantity > 0)
                .Where(s => s.TimestampUnixSeconds > 0)
                .ToList(),
        };
    }

    private static TDictionary EmptyWorldDictionary<TDictionary, T>(IEnumerable<int> worldIds)
        where TDictionary : IDictionary<int, T>
    {
        var dict = (TDictionary)Activator.CreateInstance(typeof(TDictionary));
        foreach (var worldId in worldIds)
        {
            // ReSharper disable once PossibleNullReferenceException
            dict[worldId] = default;
        }

        return dict;
    }

    private static int GetMinPricePerUnit<TPriceable>(IList<TPriceable> items) where TPriceable : IPriceable
    {
        return !items.Any() ? 0 : items.Select(s => s.PricePerUnit).Min();
    }

    private static int GetMaxPricePerUnit<TPriceable>(IList<TPriceable> items) where TPriceable : IPriceable
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
            .Select(s => s.Quantity));
    }

    /// <summary>
    /// Build properties to be serialized given user-specified json paths.
    ///
    /// Examples:
    /// <code>
    /// | fields                            | forKey | result                     |
    /// | --------------------------------- | ------ | -------------------------- |
    /// | foo.bar, bar.foo                  | null   | foo, foo.bar, bar, bar.foo |
    /// | foo.bar, foo.lorem.ipsum, bar.foo | foo    | bar, lorem, lorem.ipsum    |
    /// </code>
    /// </summary>
    /// <returns>
    /// A list of properties to be serialized or null if all properties should be serialized.
    /// </returns>
    protected static HashSet<string> BuildSerializableProperties(HashSet<string> fields, string forKey = null)
    {
        if (fields == null || fields.Count == 0)
            return null;
        var properties = new HashSet<string>();
        foreach (var f in fields)
        {
            var field = f;
            if (forKey != null)
            {
                if (field.StartsWith(forKey + "."))
                    field = field[(forKey.Length + 1)..];
                else
                    continue;
            }

            var index = field.IndexOf(".", StringComparison.Ordinal);
            if (index >= 1)
            {
                properties.Add(field[..index]); // if the field foo.bar was requested we need to serialize foo
            }

            properties.Add(field);
        }

        if (forKey != null && properties.Count == 0 &&
            fields.Contains(forKey)) // all properties of the given key were requested
            return null;
        return properties;
    }
}