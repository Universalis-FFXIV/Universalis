using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/{worldOrDc}/{itemIds}")]
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
                return Ok(currentlyShownView);
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
            return Ok(new CurrentlyShownMultiView
            {
                ItemIds = itemIdsArray.ToList(),
                Items = currentlyShownViews
                    .Where(cs => cs.Item1)
                    .Select(cs => cs.Item2)
                    .ToList(),
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                UnresolvedItemIds = unresolvedItems,
            });
        }

        protected async Task<(bool, CurrentlyShownView)> GetCurrentlyShownView(WorldDc worldDc, uint[] worldIds, uint itemId)
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
                    .Select(l =>
                    {
                        var listingView = new ListingView
                        {
                            ListingId = l.ListingId,
                            Hq = l.Hq,
                            OnMannequin = l.OnMannequin,
                            Materia = l.Materia?
                                .Select(m => new MateriaView
                                {
                                    SlotId = m.SlotId,
                                    MateriaId = m.MateriaId,
                                })
                                .ToList() ?? new List<MateriaView>(),
                            PricePerUnit = l.PricePerUnit,
                            Quantity = l.Quantity,
                            Total = l.PricePerUnit * l.Quantity,
                            DyeId = l.DyeId,
                            CreatorIdHash = l.CreatorId,
                            CreatorName = l.CreatorName,
                            IsCrafted = !string.IsNullOrEmpty(l.CreatorName),
                            LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds,
                            RetainerId = l.RetainerId,
                            RetainerName = l.RetainerName,
                            RetainerCityId = l.RetainerCityId,
                            WorldId = worldDc.IsDc ? next.WorldId : null,
                            WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,
                        };

                        using var sha256 = SHA256.Create();
                        using var dataStream1 = new MemoryStream(Encoding.UTF8.GetBytes(l.SellerId));
                        listingView.SellerIdHash = BitConverter.ToString(sha256.ComputeHash(dataStream1));

                        using var dataStream2 = new MemoryStream(Encoding.UTF8.GetBytes(l.CreatorId));
                        listingView.CreatorIdHash = BitConverter.ToString(sha256.ComputeHash(dataStream2));

                        return listingView;
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

            currentlyShown.Listings.Sort((a, b) => (int)b.PricePerUnit - (int)a.PricePerUnit);
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
                        .Select(q => (int) q))),
                StackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqListings
                        .Select(s => s.Quantity)
                        .Select(q => (int) q))),
                SaleVelocity = Statistics.WeekVelocityPerDay(currentlyShown.RecentHistory
                    .Select(s => (long) s.TimestampUnixSeconds * 1000)),
                SaleVelocityNq = Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => (long) s.TimestampUnixSeconds * 1000)),
                SaleVelocityHq = Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => (long) s.TimestampUnixSeconds * 1000)),
            };

            if (currentlyShown.Listings.Any())
            {
                view.CurrentAveragePrice = Filters
                    .RemoveOutliers(currentlyShown.Listings.Select(l => (float) l.PricePerUnit), 3)
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
                view.CurrentAveragePriceHq = Filters.RemoveOutliers(hqListings.Select(l => (float) l.PricePerUnit), 3).Average();
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
