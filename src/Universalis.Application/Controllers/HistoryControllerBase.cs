using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;
using Universalis.Application.Common;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

public class HistoryControllerBase : WorldDcControllerBase
{
    protected readonly IHistoryDbAccess History;
    
    private static readonly Counter RequestedKeys = Metrics.CreateCounter("universalis_history_requested_keys", "History Requested Keys");

    public HistoryControllerBase(IGameDataProvider gameData, IHistoryDbAccess historyDb) : base(gameData)
    {
        History = historyDb;
    }

    protected async Task<(bool, HistoryView)> GetHistoryView(
        WorldDc worldDc,
        uint[] worldIds,
        uint itemId,
        int entries,
        long statsWithin = 604800000,
        long entriesWithin = -1,
        CancellationToken cancellationToken = default)
    {
        // Record the requested compound keys
        foreach (var worldId in worldIds)
        {
            RequestedKeys.Labels($"{worldId}:{itemId}").Inc();
        }
        
        // Fetch the data
        var data = (await History.RetrieveMany(new HistoryManyQuery
        {
            WorldIds = worldIds,
            ItemId = itemId,
        }, cancellationToken)).ToList();
        var resolved = data.Count > 0;
        var worlds = GameData.AvailableWorlds();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nowSeconds = now / 1000;
        var history = await data
            .ToAsyncEnumerable()
            .AggregateAwaitAsync(new HistoryView(), async (agg, next) =>
            {
                // Handle undefined arrays
                next.Sales ??= new List<MinimizedSale>();

                agg.Sales = await next.Sales
                    .ToAsyncEnumerable()
                    .Where(s => entriesWithin < 0 || nowSeconds - s.SaleTimeUnixSeconds < entriesWithin)
                    .Where(s => s.Quantity is > 0)
                    .Select(s => new MinimizedSaleView
                    {
                        Hq = s.Hq,
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity ?? 0, // This should never be 0 since we're filtering out null and zero quantities
                        BuyerName = s.BuyerName,
                        TimestampUnixSeconds = (long)s.SaleTimeUnixSeconds,
                        WorldId = worldDc.IsDc ? next.WorldId : null,
                        WorldName = worldDc.IsDc ? worlds[next.WorldId] : null,
                    })
                    .Concat(agg.Sales.ToAsyncEnumerable())
                    .ToListAsync(cancellationToken);
                agg.LastUploadTimeUnixMilliseconds = (long)Math.Max(next.LastUploadTimeUnixMilliseconds, agg.LastUploadTimeUnixMilliseconds);

                return agg;
            }, cancellationToken);

        history.Sales.Sort((a, b) => (int)b.TimestampUnixSeconds - (int)a.TimestampUnixSeconds);

        var nqSales = history.Sales.Where(s => !s.Hq).ToList();
        var hqSales = history.Sales.Where(s => s.Hq).ToList();

        return (resolved, new HistoryView
        {
            Sales = history.Sales.Take(entries).ToList(),
            ItemId = itemId,
            WorldId = worldDc.IsWorld ? worldDc.WorldId : null,
            WorldName = worldDc.IsWorld ? worldDc.WorldName : null,
            DcName = worldDc.IsDc ? worldDc.DcName : null,
            LastUploadTimeUnixMilliseconds = history.LastUploadTimeUnixMilliseconds,
            StackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(history.Sales
                .Select(s => s.Quantity)
                .Select(q => (int)q))),
            StackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqSales
                .Select(s => s.Quantity)
                .Select(q => (int)q))),
            StackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqSales
                .Select(s => s.Quantity)
                .Select(q => (int)q))),
            SaleVelocity = Statistics.VelocityPerDay(history.Sales
                .Select(s => s.TimestampUnixSeconds * 1000), now, statsWithin),
            SaleVelocityNq = Statistics.VelocityPerDay(nqSales
                .Select(s => s.TimestampUnixSeconds * 1000), now, statsWithin),
            SaleVelocityHq = Statistics.VelocityPerDay(hqSales
                .Select(s => s.TimestampUnixSeconds * 1000), now, statsWithin),
        });
    }
}