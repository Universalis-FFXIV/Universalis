using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DataTransformations;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/history/{worldOrDc}/{itemIds}")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;
        private readonly IHistoryDbAccess _historyDb;

        public HistoryController(IGameDataProvider gameData, IHistoryDbAccess historyDb)
        {
            _gameData = gameData;
            _historyDb = historyDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string itemIds, string worldOrDc)
        {
            var itemIdsArray = itemIds.Split(',')
                .Take(100)
                .Where(itemIdStr => uint.TryParse(itemIdStr, out _))
                .Select(uint.Parse)
                .ToArray();
            if (worldOrDc.Length == 0)
                return NotFound();

            WorldDc worldDc;
            try
            {
                worldDc = WorldDc.From(worldOrDc, _gameData);
            }
            catch (Exception)
            {
                return NotFound();
            }

            var worldIds = worldDc.IsWorld ? new[] { worldDc.WorldId } : Array.Empty<uint>();
            if (worldDc.IsDc)
            {
                var dataCenter = _gameData.DataCenters().FirstOrDefault(dc => dc.Name == worldDc.DcName);
                if (dataCenter == null)
                {
                    return NotFound();
                }

                worldIds = dataCenter.WorldIds;
            }

            if (itemIdsArray.Length == 1)
            {
                var itemId = itemIdsArray[0];

                if (!_gameData.MarketableItemIds().Contains(itemId))
                {
                    return NotFound();
                }

                var (_, historyView) = await GetHistoryView(worldDc, worldIds, itemId);
                return new NewtonsoftActionResult(historyView);
            }

            var historyViewTasks = itemIdsArray
                .Select(itemId => GetHistoryView(worldDc, worldIds, itemId))
                .ToList();
            var historyViews = await Task.WhenAll(historyViewTasks);
            var unresolvedItems = historyViews
                .Where(hv => !hv.Item1)
                .Select(hv => hv.Item2.ItemId)
                .ToArray();
            return new NewtonsoftActionResult(new HistoryMultiView
            {
                ItemIds = itemIdsArray,
                Items = historyViews.Select(hv => hv.Item2).ToList(),
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                UnresolvedItemIds = unresolvedItems,
            });
        }

        private async Task<(bool, HistoryView)> GetHistoryView(WorldDc worldDc, uint[] worldIds, uint itemId)
        {
            var data = (await _historyDb.RetrieveMany(new HistoryManyQuery
            {
                WorldIds = worldIds,
                ItemId = itemId,
            })).ToList();

            var resolved = data.Count > 0;

            var history = data.Aggregate(new History(), (agg, next) =>
            {
                // Handle undefined arrays
                next.Sales ??= new List<MinimizedSale>();

                agg.Sales = agg.Sales.Concat(next.Sales).ToList();
                agg.LastUploadTimeUnixMilliseconds = Math.Max(next.LastUploadTimeUnixMilliseconds, agg.LastUploadTimeUnixMilliseconds);

                return agg;
            });

            var worlds = _gameData.AvailableWorlds();
            var nqSales = history.Sales.Where(s => !s.Hq).ToList();
            var hqSales = history.Sales.Where(s => s.Hq).ToList();
            return (resolved, new HistoryView
            {
                Sales = history.Sales
                    .Select(s => new MinimizedSaleView
                    {
                        Hq = s.Hq,
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity,
                        TimestampUnixSeconds = s.SaleTimeUnixSeconds,
                        WorldId = worldDc.IsDc ? history.WorldId : null,
                        WorldName = worldDc.IsDc ? worlds[history.WorldId] : null,
                    })
                    .ToList(),
                ItemId = itemId,
                WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
                DcName = worldDc.IsDc ? worldDc.DcName : null,
                LastUploadTimeUnixMilliseconds = history.LastUploadTimeUnixMilliseconds,
                StackSizeHistogram = Statistics.GetDistribution(history.Sales
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                StackSizeHistogramNq = Statistics.GetDistribution(nqSales
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                StackSizeHistogramHq = Statistics.GetDistribution(hqSales
                    .Select(s => s.Quantity)
                    .Select(q => (int)q)),
                RegularSaleVelocity = Statistics.WeekVelocityPerDay(history.Sales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                RegularSaleVelocityNq = Statistics.WeekVelocityPerDay(nqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
                RegularSaleVelocityHq = Statistics.WeekVelocityPerDay(hqSales
                    .Select(s => (long)s.SaleTimeUnixSeconds * 1000)),
            });
        }
    }
}
