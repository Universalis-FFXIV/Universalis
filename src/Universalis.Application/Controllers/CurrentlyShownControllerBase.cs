using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
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

        private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

        public CurrentlyShownControllerBase(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) : base(gameData)
        {
            CurrentlyShown = currentlyShownDb;
        }

        protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(WorldDc worldDc, uint[] worldIds, uint itemId, CancellationToken cancellationToken = default)
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

                    agg.Listings = await next.Listings
                        .ToAsyncEnumerable()
                        .SelectAwait(async l =>
                        {
                            var ppuWithGst = (uint)Math.Ceiling(l.PricePerUnit * 1.05);
                            var listingView = new ListingView
                            {
                                Hq = l.Hq,
                                OnMannequin = l.OnMannequin,
                                Materia = l.Materia?
                                    .Select(m => new MateriaView
                                    {
                                        SlotId = m.SlotId,
                                        MateriaId = m.MateriaId,
                                    })
                                    .ToList() ?? new List<MateriaView>(),
                                PricePerUnit = ppuWithGst,
                                Quantity = l.Quantity,
                                Total = ppuWithGst * l.Quantity,
                                DyeId = l.DyeId,
                                CreatorName = l.CreatorName ?? "",
                                IsCrafted = !string.IsNullOrEmpty(l.CreatorName),
                                LastReviewTimeUnixSeconds = (long)l.LastReviewTimeUnixSeconds,
                                RetainerName = l.RetainerName,
                                RetainerCityId = l.RetainerCityId,
                                WorldId = worldDc.IsDc ? next.WorldId : null,
                                WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,
                            };

                            using var sha256 = SHA256.Create();

                            if (!string.IsNullOrEmpty(l.CreatorId))
                            {
                                listingView.CreatorIdHash = await HashId(sha256, l.CreatorId, cancellationToken);
                            }

                            if (!string.IsNullOrEmpty(l.ListingId))
                            {
                                listingView.ListingId = await HashId(sha256, l.ListingId, cancellationToken);
                            }

                            listingView.SellerIdHash = await HashId(sha256, l.SellerId, cancellationToken);
                            listingView.RetainerId = await HashId(sha256, l.RetainerId, cancellationToken);

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
                Listings = currentlyShown.Listings,
                RecentHistory = currentlyShown.RecentHistory,
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
            };
            
            return (resolved, view);
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

        private static async Task<string> HashId(HashAlgorithm hasher, string id, CancellationToken cancellationToken = default)
        {
            var idBytes = Encoding.UTF8.GetBytes(id ?? "");
            await using var dataStream = MemoryStreamPool.GetStream(idBytes);
            return Util.BytesToString(await hasher.ComputeHashAsync(dataStream, cancellationToken));
        }
    }
}