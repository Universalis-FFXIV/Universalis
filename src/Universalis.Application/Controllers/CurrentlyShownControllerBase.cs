using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers
{
    public class CurrentlyShownControllerBase : WorldDcControllerBase
    {
        protected readonly ICurrentlyShownDbAccess CurrentlyShown;

        public CurrentlyShownControllerBase(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) : base(gameData)
        {
            CurrentlyShown = currentlyShownDb;
        }

        protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(WorldDc worldDc, uint[] worldIds, uint itemId, int nListings = int.MaxValue, int nEntries = int.MaxValue, bool noGst = false, bool? onlyHq = null, long statsWithin = 604800000, CancellationToken cancellationToken = default)
        {
            var data = (await CurrentlyShown.RetrieveMany(new CurrentlyShownManyQuery
            {
                WorldIds = worldIds,
                ItemId = itemId,
            }, cancellationToken)).ToList();

            var resolved = data.Count > 0;

            var worlds = GameData.AvailableWorlds();

            var currentlyShown = await data
                .ToAsyncEnumerable()
                .AggregateAwaitAsync(new CurrentlyShownView(), async (agg, next) =>
                {
                    // Handle undefined arrays
                    next.Listings ??= new List<Listing>();
                    next.RecentHistory ??= new List<Sale>();

                    // Convert database entities into views. Separate classes are used for the entities
                    // and the views in order to avoid any undesirable data leaking out into the public
                    // API through inheritance and to allow separate purposes for the properties to be
                    // described in the property names (e.g. CreatorIdHash in the view and CreatorId in
                    // the database entity).
                    agg.Listings = await next.Listings
                        .ToAsyncEnumerable()
                        .SelectAwait(async l =>
                        {
                            var listingView = await Util.ListingToView(l, noGst, cancellationToken);
                            listingView.WorldId = worldDc.IsDc ? next.WorldId : null;
                            listingView.WorldName = worldDc.IsDc ? worlds[next.WorldId] : null;
                            return listingView;
                        })
                        .Concat(agg.Listings.ToAsyncEnumerable())
                        .ToListAsync(cancellationToken);

                    agg.RecentHistory = await next.RecentHistory
                        .ToAsyncEnumerable()
                        .Select(s => new SaleView
                        {
                            Hq = s.Hq,
                            PricePerUnit = s.PricePerUnit,
                            Quantity = s.Quantity,
                            Total = s.PricePerUnit * s.Quantity,
                            TimestampUnixSeconds = (long)s.TimestampUnixSeconds,
                            BuyerName = s.BuyerName,
                            WorldId = worldDc.IsDc ? next.WorldId : null,
                            WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,
                        })
                        .Where(s => s.PricePerUnit > 0)
                        .Where(s => s.Quantity > 0)
                        .Where(s => s.TimestampUnixSeconds > 0)
                        .Concat(agg.RecentHistory.ToAsyncEnumerable())
                        .ToListAsync(cancellationToken);
                    agg.LastUploadTimeUnixMilliseconds = (long)Math.Max(next.LastUploadTimeUnixMilliseconds, agg.LastUploadTimeUnixMilliseconds);

                    if (worldDc.IsDc)
                    {
                        agg.WorldUploadTimes ??= new Dictionary<uint, long>();
                        agg.WorldUploadTimes[next.WorldId] = (long)next.LastUploadTimeUnixMilliseconds;
                    }

                    return agg;
                }, cancellationToken);

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
                SaleVelocity = GetSaleVelocity(currentlyShown.RecentHistory),
                SaleVelocityNq = GetSaleVelocity(nqSales),
                SaleVelocityHq = GetSaleVelocity(hqSales),
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
                WorldUploadTimes = worldDc.IsWorld ? null : currentlyShown.WorldUploadTimes ?? EmptyWorldDictionary<Dictionary<uint, long>, long>(worldIds),
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

        private static float GetSaleVelocity(IEnumerable<SaleView> sales)
        {
            return Statistics.WeekVelocityPerDay(sales
                .Select(s => s.TimestampUnixSeconds * 1000));
        }

        private static IDictionary<int, int> GetListingsDistribution(IEnumerable<ListingView> listings)
        {
            return Statistics.GetDistribution(listings
                .Select(s => s.Quantity)
                .Select(q => (int)q));
        }
    }
}