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

        private static readonly RecyclableMemoryStreamManager memoryStreamPool = new();

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
                                var creatorIdBytes = Encoding.UTF8.GetBytes(l.CreatorId);
                                await using var dataStream = memoryStreamPool.GetStream();
                                await dataStream.WriteAsync(creatorIdBytes.AsMemory(0, creatorIdBytes.Length), cancellationToken);
                                listingView.CreatorIdHash = Util.BytesToString(await sha256.ComputeHashAsync(dataStream, cancellationToken));
                            }

                            if (!string.IsNullOrEmpty(l.ListingId))
                            {
                                var listingIdBytes = Encoding.UTF8.GetBytes(l.ListingId);
                                await using var dataStream = memoryStreamPool.GetStream();
                                await dataStream.WriteAsync(listingIdBytes.AsMemory(0, listingIdBytes.Length), cancellationToken);
                                listingView.ListingId = Util.BytesToString(await sha256.ComputeHashAsync(dataStream, cancellationToken));
                            }

                            {
                                var sellerIdBytes = Encoding.UTF8.GetBytes(l.SellerId ?? "");
                                await using var dataStream = memoryStreamPool.GetStream();
                                await dataStream.WriteAsync(sellerIdBytes.AsMemory(0, sellerIdBytes.Length), cancellationToken);
                                listingView.SellerIdHash = Util.BytesToString(await sha256.ComputeHashAsync(dataStream, cancellationToken));
                            }

                            {
                                var retainerIdBytes = Encoding.UTF8.GetBytes(l.RetainerId ?? "");
                                await using var dataStream = memoryStreamPool.GetStream();
                                await dataStream.WriteAsync(retainerIdBytes.AsMemory(0, retainerIdBytes.Length), cancellationToken);
                                listingView.RetainerId = Util.BytesToString(await sha256.ComputeHashAsync(dataStream, cancellationToken));
                            }

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
                StackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(currentlyShown.Listings
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))),
                StackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqListings
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))),
                StackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqListings
                        .Select(s => s.Quantity)
                        .Select(q => (int)q))),
                SaleVelocity = Statistics.WeekVelocityPerDay(currentlyShown.RecentHistory
                    .Select(s => s.TimestampUnixSeconds * 1000)),
                SaleVelocityNq = Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => s.TimestampUnixSeconds * 1000)),
                SaleVelocityHq = Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => s.TimestampUnixSeconds * 1000)),
            };

            if (currentlyShown.Listings.Any())
            {
                view.CurrentAveragePrice = Filters
                    .RemoveOutliers(currentlyShown.Listings.Select(l => (float)l.PricePerUnit), 3)
                    .Average();
                view.MinPrice = currentlyShown.Listings.Select(s => s.PricePerUnit).Min();
                view.MaxPrice = currentlyShown.Listings.Select(s => s.PricePerUnit).Max();
            }

            if (nqListings.Any())
            {
                view.CurrentAveragePriceNq = Filters.RemoveOutliers(nqListings.Select(l => (float)l.PricePerUnit), 3).Average();
                view.MinPriceNq = nqListings.Select(s => s.PricePerUnit).Min();
                view.MaxPriceNq = nqListings.Select(s => s.PricePerUnit).Max();
            }

            if (hqListings.Any())
            {
                view.CurrentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(l => (float)l.PricePerUnit), 3).Average();
                view.MinPriceHq = hqListings.Select(s => s.PricePerUnit).Min();
                view.MaxPriceHq = hqListings.Select(s => s.PricePerUnit).Max();
            }

            if (currentlyShown.RecentHistory.Any())
            {
                view.AveragePrice = Filters.RemoveOutliers(currentlyShown.RecentHistory.Select(s => (float)s.PricePerUnit), 3).Average();
            }

            if (nqSales.Any())
            {
                view.AveragePriceNq = Filters.RemoveOutliers(nqSales.Select(s => (float)s.PricePerUnit), 3).Average();
            }

            if (hqSales.Any())
            {
                view.AveragePriceHq = Filters.RemoveOutliers(hqSales.Select(s => (float)s.PricePerUnit), 3).Average();
            }

            return (resolved, view);
        }
    }
}