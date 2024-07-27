using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V2;
using Universalis.Common.GameData;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V2;

[ApiController]
[ApiVersion("2")]
[Route("api/v{version:apiVersion}/aggregated/{worldDcRegion}/{itemIds}")]
public class AggregatedMarketBoardDataController : WorldDcRegionControllerBase
{
    private readonly IAggregatedMarketBoardDataDbAccess _dbAccess;
    private readonly IWorldToDcRegion _worldToDcRegion;

    public AggregatedMarketBoardDataController(IGameDataProvider gameData, IAggregatedMarketBoardDataDbAccess aggregatedMarketBoardDataDbAccess, IWorldToDcRegion worldToDcRegion) : base(gameData)
    {
        _dbAccess = aggregatedMarketBoardDataDbAccess;
        _worldToDcRegion = worldToDcRegion;
    }

    /// <summary>
    /// Retrieves aggregated market board data for the given items.
    /// Up to 100 item IDs can be comma-separated in order to retrieve data for multiple items at once.
    /// </summary>
    /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
    /// <param name="worldDcRegion">The world, data center, or region to retrieve data for. This may be an ID or a name. Regions should be specified as Japan, Europe, North-America, Oceania, China, or 中国.</param>
    /// <param name="userAgent"></param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="400">The parameters were invalid.</response>
    /// <response code="404">
    /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
    /// will not trigger this. Instead, the returned list of unresolved item IDs will contain the invalid item ID or IDs.
    /// </response>
    [HttpGet]
    [ApiTag("Current item price")]
    [ProducesResponseType(typeof(AggregatedMarketBoardData), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(
        string itemIds,
        string worldDcRegion,
        [FromHeader(Name = "User-Agent")] string userAgent = "",
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("AggregatedMarketBoardDataController.Get");
        activity?.AddTag("itemIds", itemIds);
        activity?.AddTag("worldDcRegion", worldDcRegion);
        UserAgentMetrics.RecordUserAgentRequest(userAgent, nameof(AggregatedMarketBoardDataController), activity);

        if (itemIds == null || worldDcRegion == null)
        {
            return BadRequest();
        }

        // Parameter parsing
        var itemIdsArray = InputProcessing.ParseIdList(itemIds)
            .Take(100)
            .ToArray();

        if (!TryGetWorldDc(worldDcRegion, out var worldDc))
        {
            return NotFound();
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        var results = new List<AggregatedMarketBoardData.Result>();
        var failedItems = new List<int>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tradeVelocityCalculationRange = today.AddDays(-3);
        int? worldId;
        string dcName, regionName;
        if (worldDc.IsWorld)
        {
            worldId = worldDc.WorldId;
            (dcName, regionName) = _worldToDcRegion.Get(worldDc.WorldId);
        }
        else if (worldDc.IsDc)
        {
            worldId = null;
            dcName = worldDc.DcName;
            regionName = worldDc.RegionName;
        }
        else
        {
            worldId = null;
            dcName = null;
            regionName = worldDc.RegionName;
        }

        foreach (var itemId in itemIdsArray)
        {
            if (!GameData.MarketableItemIds().Contains(itemId))
            {
                failedItems.Add(itemId);
                continue;
            }

            try
            {
                cts.Token.ThrowIfCancellationRequested();
                var minListing = await FetchMinListing(itemId, worldId, dcName, regionName);
                var uploadTimes = await FetchUploadTimes(itemId, worldId, minListing, cts);
                var worldVelocity = await _dbAccess.RetrieveUnitTradeVelocity(worldId.ToString(), itemId, tradeVelocityCalculationRange, today, cts.Token);
                var dcVelocity = await _dbAccess.RetrieveUnitTradeVelocity(dcName, itemId, tradeVelocityCalculationRange, today, cts.Token);
                var regionVelocity = await _dbAccess.RetrieveUnitTradeVelocity(regionName, itemId, tradeVelocityCalculationRange, today, cts.Token);

                var nq = await GetAggregatedResult(worldId, itemId, dcName, regionName, worldVelocity.Nq, dcVelocity.Nq, regionVelocity.Nq, minListing, false);
                var hq = await GetAggregatedResult(worldId, itemId, dcName, regionName, worldVelocity.Hq, dcVelocity.Hq, regionVelocity.Hq, minListing, true);

                results.Add(new AggregatedMarketBoardData.Result(itemId, nq, hq, uploadTimes));
            }
            catch (OperationCanceledException e)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout);
            }
            catch (Exception e)
            {
                failedItems.Add(itemId);
            }
        }

        if (results.Count == 0)
            return BadRequest();

        return Ok(new AggregatedMarketBoardData(results, failedItems));
    }

    private async Task<List<AggregatedMarketBoardData.WorldUploadTime>> FetchUploadTimes(int itemId, int? worldId, MinListing minListing, CancellationTokenSource cts)
    {
        var worldUploadTimes = await _dbAccess.RetrieveWorldUploadTimes(itemId, cts.Token,
            worldId ?? 0, minListing.Dc?.Nq?.WorldId ?? 0, minListing.Dc?.Hq?.WorldId ?? 0, minListing.Region?.Nq?.WorldId ?? 0, minListing.Region?.Hq?.WorldId ?? 0);
        return worldUploadTimes
            .Select(w => new AggregatedMarketBoardData.WorldUploadTime(w.WorldId, new DateTimeOffset(w.LastUploadTime).ToUnixTimeMilliseconds()))
            .ToList();
    }

    private async Task<MinListing> FetchMinListing(int itemId, int? worldId, string dcName, string regionName)
    {
        if (worldId != null)
            return await _dbAccess.GetMinListing(worldId.Value, itemId);
        var dc = dcName != null ? await _dbAccess.GetMinListing(dcName, itemId) : null;
        var region = await _dbAccess.GetMinListing(regionName, itemId);
        return new MinListing(null, dc, region);
    }

    private async Task<AggregatedMarketBoardData.AggregatedResult> GetAggregatedResult(int? worldId, int itemId, string dcName, string regionName, TradeVelocity worldVelocity, TradeVelocity dcVelocity,
        TradeVelocity regionVelocity, MinListing minListing, bool hq)
    {
        var recentPurchaseWorld = worldId != null ? await _dbAccess.GetMostRecentSaleInWorld(worldId.Value, itemId, hq) : null;
        var recentPurchaseDc = dcName != null ? await _dbAccess.GetMostRecentSaleInDatacenterOrRegion(dcName, itemId, hq) : null;
        var recentPurchaseRegion = await _dbAccess.GetMostRecentSaleInDatacenterOrRegion(regionName, itemId, hq);

        return new AggregatedMarketBoardData.AggregatedResult(
            GetMinListing(minListing, hq ? e => e?.Hq : e => e?.Nq),
            null,
            RecentPurchase(recentPurchaseWorld, recentPurchaseDc, recentPurchaseRegion),
            GetAverageSalePrice(worldVelocity, dcVelocity, regionVelocity),
            GetDailySaleVelocity(worldVelocity, dcVelocity, regionVelocity));
    }

    private static AggregatedMarketBoardData.MinListing GetMinListing(MinListing minListing, Func<MinListing.Entry, MinListing.Price> selector)
    {
        return new AggregatedMarketBoardData.MinListing(
            selector(minListing?.World) is var (_, wPrice) ? new AggregatedMarketBoardData.MinListing.Entry(wPrice, null) : null,
            selector(minListing?.Dc) is var (dWorld, dPrice) ? new AggregatedMarketBoardData.MinListing.Entry(dPrice, dWorld) : null,
            selector(minListing?.Region) is var (rWorld, rPrice) ? new AggregatedMarketBoardData.MinListing.Entry(rPrice, rWorld) : null);
    }

    private static AggregatedMarketBoardData.RecentPurchase RecentPurchase(Sale recentPurchaseWorld, Sale recentPurchaseDc, Sale recentPurchaseRegion)
    {
        return new AggregatedMarketBoardData.RecentPurchase(
            recentPurchaseWorld != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(
                recentPurchaseWorld.PricePerUnit, new DateTimeOffset(recentPurchaseWorld.SaleTime).ToUnixTimeMilliseconds(), null) : null,
            recentPurchaseDc != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(
                recentPurchaseDc.PricePerUnit, new DateTimeOffset(recentPurchaseDc.SaleTime).ToUnixTimeMilliseconds(), recentPurchaseDc.WorldId) : null,
            recentPurchaseRegion != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(
                recentPurchaseRegion.PricePerUnit, new DateTimeOffset(recentPurchaseRegion.SaleTime).ToUnixTimeMilliseconds(), recentPurchaseRegion.WorldId) : null);
    }

    private static AggregatedMarketBoardData.DailySaleVelocity GetDailySaleVelocity(TradeVelocity worldVelocity, TradeVelocity dcVelocity, TradeVelocity regionVelocity)
    {
        return new AggregatedMarketBoardData.DailySaleVelocity(
            worldVelocity != null ? new AggregatedMarketBoardData.DailySaleVelocity.Entry(worldVelocity.AvgSalesPerDay) : null,
            dcVelocity != null ? new AggregatedMarketBoardData.DailySaleVelocity.Entry(dcVelocity.AvgSalesPerDay) : null,
            regionVelocity != null ? new AggregatedMarketBoardData.DailySaleVelocity.Entry(regionVelocity.AvgSalesPerDay) : null);
    }

    private static AggregatedMarketBoardData.AverageSalePrice GetAverageSalePrice(TradeVelocity worldVelocity, TradeVelocity dcVelocity, TradeVelocity regionVelocity)
    {
        return new AggregatedMarketBoardData.AverageSalePrice(
            worldVelocity is { Quantity: > 0 } ? new AggregatedMarketBoardData.AverageSalePrice.Entry(worldVelocity.SumSales / (double)worldVelocity.Quantity) : null,
            dcVelocity is { Quantity: > 0 } ? new AggregatedMarketBoardData.AverageSalePrice.Entry(dcVelocity.SumSales / (double)dcVelocity.Quantity) : null,
            regionVelocity is { Quantity: > 0 } ? new AggregatedMarketBoardData.AverageSalePrice.Entry(regionVelocity.SumSales / (double)regionVelocity.Quantity) : null);
    }
}
