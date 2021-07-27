using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DataTransformations;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/{worldOrDc}/{itemIds}")]
    [ApiController]
    public class CurrentlyShownController : WorldDcControllerBase
    {
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;

        public CurrentlyShownController(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) : base(gameData)
        {
            _currentlyShownDb = currentlyShownDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string itemIds, string worldOrDc)
        {
            // Parameter parsing
            var itemIdsArray = InputProcessing.ParseIdList(itemIds)
                .Take(100)
                .ToArray();

            if (!TryGetWorldDc(worldOrDc, out var worldDc))
            {
                return NotFound();
            }

            if (!TryGetWorldIds(worldDc, out var worldIds))
            {
                return NotFound();
            }

            if (itemIdsArray.Length == 1)
            {
                var itemId = itemIdsArray[0];

                if (!GameData.MarketableItemIds().Contains(itemId))
                {
                    return NotFound();
                }

                var (_, currentlyShownView) = await GetCurrentlyShownView(worldDc, worldIds, itemId);
                return new NewtonsoftActionResult(currentlyShownView);
            }

            // Multi-item handling
            var currentlyShownViewTasks = itemIdsArray
                .Select(itemId => GetCurrentlyShownView(worldDc, worldIds, itemId))
                .ToList();
            var currentlyShownViews = await Task.WhenAll(currentlyShownViewTasks);
            var unresolvedItems = currentlyShownViews
                .Where(cs => !cs.Item1)
                .Select(cs => cs.Item2.ItemId)
                .ToArray();
            return new NewtonsoftActionResult(new CurrentlyShownMultiView
            {
                ItemIds = itemIdsArray,
                Items = currentlyShownViews.Select(cs => cs.Item2).ToList(),
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                UnresolvedItemIds = unresolvedItems,
            });
        }

        private async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(WorldDc worldDc, uint[] worldIds, uint itemId)
        {
            var data = (await _currentlyShownDb.RetrieveMany(new CurrentlyShownManyQuery
            {
                WorldIds = worldIds,
                ItemId = itemId,
            })).ToList();

            var resolved = data.Count > 0;

            var worlds = GameData.AvailableWorlds();

            var currentlyShown = data.Aggregate(new CurrentlyShownView(), (agg, next) =>
            {
                // Handle undefined arrays
                next.Listings ??= new List<Listing>();
                next.RecentHistory ??= new List<Sale>();

                agg.Listings = next.Listings
                    .Select(s => new ListingView
                    {
                        ListingId = s.ListingId,
                        Hq = s.Hq,
                        OnMannequin = s.OnMannequin,
                        Materia = s.Materia?
                            .Select(m => new MateriaView
                            {
                                SlotId = m.SlotId,
                                MateriaId = m.MateriaId,
                            })
                            .ToList() ?? new List<MateriaView>(),
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity,
                        Total = s.PricePerUnit * s.Quantity,
                        DyeId = s.DyeId,
                        CreatorIdHash = s.CreatorIdHash,
                        CreatorName = s.CreatorName,
                        IsCrafted = !string.IsNullOrEmpty(s.CreatorName),
                        LastReviewTimeUnixSeconds = s.LastReviewTimeUnixSeconds,
                        RetainerId = s.RetainerId,
                        RetainerName = s.RetainerName,
                        RetainerCityId = s.RetainerCityId,
                        SellerIdHash = s.SellerIdHash,
                        WorldId = worldDc.IsDc ? next.WorldId : null,
                        WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,

                    })
                    .Concat(agg.Listings)
                    .ToList();
                agg.RecentHistory = next.RecentHistory
                    .Select(s => new SaleView
                    {
                        Hq = s.Hq,
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity,
                        Total = s.PricePerUnit * s.Quantity,
                        TimestampUnixSeconds = s.TimestampUnixSeconds,
                        BuyerName = s.BuyerName,
                        WorldId = worldDc.IsDc ? next.WorldId : null,
                        WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,
                    })
                    .Concat(agg.RecentHistory)
                    .ToList();
                agg.LastUploadTimeUnixMilliseconds = Math.Max(next.LastUploadTimeUnixMilliseconds, agg.LastUploadTimeUnixMilliseconds);

                return agg;
            });

            var nqListings = currentlyShown.Listings.Where(l => !l.Hq).ToList();
            var hqListings = currentlyShown.Listings.Where(l => l.Hq).ToList();
            var nqSales = currentlyShown.RecentHistory.Where(s => !s.Hq).ToList();
            var hqSales = currentlyShown.RecentHistory.Where(s => s.Hq).ToList();
            return (resolved, new CurrentlyShownView
            {
                Listings = currentlyShown.Listings,
                RecentHistory = currentlyShown.RecentHistory,
                ItemId = itemId,
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                LastUploadTimeUnixMilliseconds = currentlyShown.LastUploadTimeUnixMilliseconds,
                CurrentAveragePrice = Filters.RemoveOutliers(currentlyShown.Listings.Select(l => (float)l.PricePerUnit), 3).Average(),
                CurrentAveragePriceNq = Filters.RemoveOutliers(nqListings.Select(l => (float)l.PricePerUnit), 3).Average(),
                CurrentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(l => (float)l.PricePerUnit), 3).Average(),
                AveragePrice = Filters.RemoveOutliers(currentlyShown.RecentHistory.Select(s => (float)s.PricePerUnit), 3).Average(),
                AveragePriceNq = Filters.RemoveOutliers(nqSales.Select(s => (float)s.PricePerUnit), 3).Average(),
                AveragePriceHq = Filters.RemoveOutliers(hqSales.Select(s => (float)s.PricePerUnit), 3).Average(),
                MinPrice = currentlyShown.RecentHistory.Select(s => s.PricePerUnit).Min(),
                MinPriceNq = nqSales.Select(s => s.PricePerUnit).Min(),
                MinPriceHq = hqSales.Select(s => s.PricePerUnit).Min(),
                MaxPrice = currentlyShown.RecentHistory.Select(s => s.PricePerUnit).Max(),
                MaxPriceNq = nqSales.Select(s => s.PricePerUnit).Max(),
                MaxPriceHq = hqSales.Select(s => s.PricePerUnit).Max(),
                StackSizeHistogram = Statistics.GetDistribution(currentlyShown.RecentHistory
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                StackSizeHistogramNq = Statistics.GetDistribution(nqSales
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                StackSizeHistogramHq = Statistics.GetDistribution(hqSales
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                RegularSaleVelocity = Statistics.WeekVelocityPerDay(currentlyShown.RecentHistory
                    .Select(s => (long)s.TimestampUnixSeconds * 1000)),
                RegularSaleVelocityNq = Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => (long)s.TimestampUnixSeconds * 1000)),
                RegularSaleVelocityHq = Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => (long)s.TimestampUnixSeconds * 1000)),
            });
        }
    }
}
