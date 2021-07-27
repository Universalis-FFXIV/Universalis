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
            var itemIdsArray = itemIds.Split(',').Select(uint.Parse).ToArray();
            var itemId = itemIdsArray[0];

            if (!_gameData.MarketableItemIds().Contains(itemId) || worldOrDc.Length == 0)
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

            var data = await _historyDb.RetrieveMany(new HistoryManyQuery
            {
                WorldIds = worldIds,
                ItemId = itemId,
            });

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
            return new NewtonsoftActionResult(new HistoryView
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
