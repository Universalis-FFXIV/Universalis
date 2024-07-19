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

    protected async Task<(int[], List<CurrentlyShownView>)> GetCurrentlyShownViews(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int[] itemIds,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        var itemsSerializableProperties = BuildSerializableProperties(fields, "items");
        var currentlyShownViews = await GetViewBatched(
            worldDcRegion, worldIds, itemIds, nListings, nEntries, onlyHq, statsWithin, entriesWithin,
            itemsSerializableProperties, cancellationToken);
        var unresolvedItemIds = currentlyShownViews
            .Where(cs => !cs.Item1)
            .Select(cs => cs.Item2.ItemId)
            .ToArray();
        var resolvedItems = currentlyShownViews
            .Where(cs => cs.Item1)
            .Select(cs => cs.Item2)
            .ToList();
        return (unresolvedItemIds, resolvedItems);
    }

    protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
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
            return await GetView(worldDcRegion, worldIds[0], itemId, nListings, nEntries, onlyHq, statsWithin,
                entriesWithin, fields, cancellationToken);
        }

        var batches = await GetViewBatched(worldDcRegion, worldIds, new[] { itemId }, nListings, nEntries, onlyHq,
            statsWithin, entriesWithin, fields, cancellationToken);
        return batches[0];
    }

    private async Task<(bool, CurrentlyShownView)> GetView(
        WorldDcRegion worldDcRegion,
        int worldId,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.GetView");

        if (!GameData.MarketableItemIds().Contains(itemId))
        {
            return (false, ErrorView(worldDcRegion, itemId, fields));
        }

        var currentlyShown = await FetchData(worldId, itemId, nEntries, cancellationToken);

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
                l.Tax = Util.CalculateTax(l.PricePerUnit, l.Quantity);
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

        return (true, HydrateCurrentlyShownView(currentlyShown, worldDcRegion, itemId, null, fields,
            nListings, nEntries, now, statsWithin, onlyHq));
    }

    private async Task<(bool, CurrentlyShownView)[]> GetViewBatched(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int[] itemIds,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.GetViewBatched");

        if (!HasAnyValidItemIds(itemIds))
        {
            return itemIds.Select(itemId => (false, ErrorView(worldDcRegion, itemId, fields))).ToArray();
        }

        var data = await FetchDataBatched(worldIds, itemIds, nEntries, cancellationToken);
        var dataByItemId = data
            .GroupBy(view => view.ItemId)
            .Select(itemIdViews => CollateWorldViews(itemIdViews, worldDcRegion, worldIds, itemIdViews.Key, nListings,
                nEntries, onlyHq, statsWithin, entriesWithin, fields));

        return dataByItemId.ToArray();
    }

    /// <summary>
    /// Flattens a series of result pages for single world/item pairs into single result pages for an item across
    /// multiple worlds. This is used for collecting DC- or region-wide results into a single response body.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="worldDcRegion"></param>
    /// <param name="worldIds"></param>
    /// <param name="itemId"></param>
    /// <param name="nListings"></param>
    /// <param name="nEntries"></param>
    /// <param name="onlyHq"></param>
    /// <param name="statsWithin"></param>
    /// <param name="entriesWithin"></param>
    /// <param name="fields"></param>
    /// <returns></returns>
    private (bool, CurrentlyShownView) CollateWorldViews(IEnumerable<CurrentlyShownView> data,
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int itemId,
        int nListings = int.MaxValue,
        int nEntries = int.MaxValue,
        bool? onlyHq = null,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        HashSet<string> fields = null)
    {
        var worlds = GameData.AvailableWorlds();

        var listingSerializableProperties = BuildSerializableProperties(fields, "listings");
        var recentHistorySerializableProperties = BuildSerializableProperties(fields, "recentHistory");

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;

        var (worldUploadTimes, currentlyShown) = data
            .Aggregate(
                (EmptyWorldDictionary<Dictionary<int, long>, long>(worldIds), EmptyView()),
                (agg, next) =>
                {
                    if (next.WorldId == null)
                    {
                        return agg;
                    }

                    var (aggWorldUploadTimes, aggData) = agg;
                    return ReduceViews(worldDcRegion, entriesWithin, nowSeconds, listingSerializableProperties,
                        recentHistorySerializableProperties, worlds, aggWorldUploadTimes, aggData, next);
                });

        if (currentlyShown.LastUploadTimeUnixMilliseconds == 0)
        {
            return (false, ErrorView(worldDcRegion, itemId, fields));
        }

        return (true, HydrateCurrentlyShownView(currentlyShown, worldDcRegion, itemId, worldUploadTimes, fields,
            nListings, nEntries, now, statsWithin, onlyHq));
    }

    private bool HasAnyValidItemIds(params int[] itemIds)
    {
        return itemIds.Any(itemId => GameData.MarketableItemIds().Contains(itemId));
    }

    private static CurrentlyShownView ErrorView(WorldDcRegion worldDcRegion, int itemId, HashSet<string> fields)
    {
        return new CurrentlyShownView
        {
            ItemId = itemId,
            WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
            WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
            DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
            RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
            SerializableProperties = BuildSerializableProperties(fields),
        };
    }

    private static CurrentlyShownView EmptyView()
    {
        return new CurrentlyShownView
        {
            Listings = new List<ListingView>(),
            RecentHistory = new List<SaleView>(),
        };
    }

    private static CurrentlyShownView HydrateCurrentlyShownView(
        CurrentlyShownView currentlyShown,
        WorldDcRegion worldDcRegion,
        int itemId,
        Dictionary<int, long> worldUploadTimes,
        HashSet<string> fields,
        int nListings,
        int nSales,
        long now,
        long statsWithin,
        bool? onlyHq)
    {
        currentlyShown.Listings.Sort((a, b) => a.PricePerUnit - b.PricePerUnit);
        currentlyShown.RecentHistory.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

        var nqListings = currentlyShown.Listings.Where(l => !l.Hq).ToList();
        var hqListings = currentlyShown.Listings.Where(l => l.Hq).ToList();
        var nqSales = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
        var hqSales = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();

        var requestedListings = currentlyShown.Listings.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nListings)
            .ToList();
        var requestedHistory = currentlyShown.RecentHistory.Where(l => onlyHq == null || onlyHq == l.Hq).Take(nSales)
            .ToList();

        return new CurrentlyShownView
        {
            Listings = requestedListings,
            RecentHistory = requestedHistory,
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
            ListingsCount = requestedListings.Count,
            RecentHistoryCount = requestedHistory.Count,
            UnitsForSale = requestedListings.Sum(listing => listing.Quantity),
            UnitsSold = requestedHistory.Sum(sale => sale.Quantity),
            SerializableProperties = BuildSerializableProperties(fields),
        };
    }

    private static (Dictionary<int, long>, CurrentlyShownView) ReduceViews(
        WorldDcRegion worldDcRegion,
        long entriesWithin,
        long nowSeconds,
        HashSet<string> listingSerializableProperties,
        HashSet<string> recentHistorySerializableProperties,
        IReadOnlyDictionary<int, string> worldNames,
        Dictionary<int, long> aggWorldUploadTimes,
        CurrentlyShownView aggData,
        CurrentlyShownView next)
    {
        if (!next.WorldId.HasValue)
        {
            return (aggWorldUploadTimes, aggData);
        }

        // Convert database entities into views. Separate classes are used for the entities
        // and the views in order to avoid any undesirable data leaking out into the public
        // API through inheritance and to allow separate purposes for the properties to be
        // described in the property names (e.g. CreatorIdHash in the view and CreatorId in
        // the database entity).

        aggData.Listings.AddRange(next.Listings
            .Select(l =>
            {
                l.Tax = Util.CalculateTax(l.PricePerUnit, l.Quantity);
                l.WorldId = !worldDcRegion.IsWorld ? next.WorldId : null;
                l.WorldName = !worldDcRegion.IsWorld ? worldNames[next.WorldId.Value] : null;
                l.SerializableProperties = listingSerializableProperties;
                return l;
            }));

        aggData.RecentHistory.AddRange(next.RecentHistory
            .Where(s => entriesWithin < 0 || nowSeconds - s.TimestampUnixSeconds < entriesWithin)
            .Select(s =>
            {
                s.WorldId = !worldDcRegion.IsWorld ? next.WorldId : null;
                s.WorldName = !worldDcRegion.IsWorld ? worldNames[next.WorldId.Value] : null;
                s.SerializableProperties = recentHistorySerializableProperties;
                return s;
            }));

        aggData.LastUploadTimeUnixMilliseconds = Math.Max(next.LastUploadTimeUnixMilliseconds,
            aggData.LastUploadTimeUnixMilliseconds);

        aggWorldUploadTimes[next.WorldId.Value] = next.LastUploadTimeUnixMilliseconds;

        return (aggWorldUploadTimes, aggData);
    }

    private async Task<CurrentlyShownView> FetchData(int worldId, int itemId, int nEntries,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.FetchData");

        var csTask = CurrentlyShown.Retrieve(new CurrentlyShownQuery { WorldId = worldId, ItemId = itemId },
            cancellationToken);
        var hTask = History.Retrieve(new HistoryQuery { WorldId = worldId, ItemId = itemId, Count = nEntries },
            cancellationToken);
        await Task.WhenAll(csTask, hTask);

        var cs = await csTask;
        var history = await hTask;

        return BuildPartialView(cs ?? new CurrentlyShown(), history ?? new History(), worldId, itemId);
    }

    private async Task<IEnumerable<CurrentlyShownView>> FetchDataBatched(IEnumerable<int> worlds,
        IEnumerable<int> items, int nEntries, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.FetchDataBatched");

        var worldIds = worlds.ToList();
        var itemIds = items.ToList();
        var worldItemPairs = worldIds.SelectMany(worldId =>
                itemIds.Select(itemId => new WorldItemPair(worldId, itemId)))
            .ToList();

        var csTask = CurrentlyShown.RetrieveMany(new CurrentlyShownManyQuery { WorldIds = worldIds, ItemIds = itemIds },
            cancellationToken);
        var hTask = History.RetrieveMany(
            new HistoryManyQuery { WorldIds = worldIds, ItemIds = itemIds, Count = nEntries }, cancellationToken);
        await Task.WhenAll(csTask, hTask);

        var cs = await csTask;
        var csDict = CollectListings(cs);
        var history = await hTask;
        var historyDict = CollectSales(history);

        return worldItemPairs
            .Select(wi => BuildPartialView(
                csDict.TryGetValue(new WorldItemPair(wi.WorldId, wi.ItemId), out var c) ? c : new CurrentlyShown(),
                historyDict.TryGetValue(new WorldItemPair(wi.WorldId, wi.ItemId), out var h) ? h : new History(),
                wi.WorldId,
                wi.ItemId));
    }

    private static Dictionary<WorldItemPair, CurrentlyShown> CollectListings(IEnumerable<CurrentlyShown> listings)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.CollectListings");
        var result = listings.ToDictionary(o => new WorldItemPair(o.WorldId, o.ItemId));
        activity?.AddTag("db.resultCount", result.Count);
        return result;
    }

    private static Dictionary<WorldItemPair, History> CollectSales(IEnumerable<History> sales)
    {
        using var activity = Util.ActivitySource.StartActivity("CurrentlyShownBase.CollectSales");
        var result = sales.ToDictionary(o => new WorldItemPair(o.WorldId, o.ItemId));
        activity?.AddTag("db.resultCount", result.Count);
        return result;
    }

    private static CurrentlyShownView BuildPartialView(
        CurrentlyShown currentlyShown,
        History history,
        int worldId,
        int itemId)
    {
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

        return items.Select(s => (float)s.PricePerUnit).Average();
    }

    private static float GetSaleVelocity(IEnumerable<SaleView> sales, long unixNowMs, long statsWithinMs)
    {
        return Statistics.VelocityPerDay(sales
            .Select(s => (s.TimestampUnixSeconds * 1000, s.Quantity)), unixNowMs, statsWithinMs);
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