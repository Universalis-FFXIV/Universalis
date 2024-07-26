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

    private readonly IListingStore _listingStore;
    private readonly ISaleStore _saleStore;
    private readonly IMarketItemStore _marketItemStore;
    private readonly IWorldToDcRegion _worldToDcRegion;

    public AggregatedMarketBoardDataController(IGameDataProvider gameData, IListingStore listingStore, ISaleStore saleStore, IMarketItemStore marketItemStore, IWorldToDcRegion worldToDcRegion) : base(gameData)
    {
        _listingStore = listingStore;
        _saleStore = saleStore;
        _marketItemStore = marketItemStore;
        _worldToDcRegion = worldToDcRegion;
    }

    [HttpGet]
    [ApiTag("Current item price")]
    [ProducesResponseType(typeof(AggregatedMarketBoardData), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(
        string itemIds,
        string worldDcRegion,
        [FromQuery] string scope = "",
        [FromQuery] string include = "",
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
        var worldId = worldDc.WorldId;
        var (dcName, regionName) = _worldToDcRegion.Get(worldId);

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
                var minListing = await _listingStore.GetMinListing(worldId, itemId);
                var uploadTimes = (await _marketItemStore.RetrieveMany(new MarketItemManyQuery()
                    {
                        ItemIds = new[] { itemId },
                        WorldIds = new[] { worldId, minListing.Dc?.Nq?.WorldId ?? 0, minListing.Dc?.Hq?.WorldId ?? 0, minListing.Region?.Nq?.WorldId ?? 0, minListing.Region?.Hq?.WorldId ?? 0 },
                    }, cts.Token))
                    .Select(w => new AggregatedMarketBoardData.WorldUploadTime(w.WorldId, new DateTimeOffset(w.LastUploadTime).ToUnixTimeMilliseconds())).ToList();

                var recentPurchaseWorldNq = await _saleStore.GetMostRecentSaleInWorld(worldId, itemId, false);
                var recentPurchaseDcNq = await _saleStore.GetMostRecentSaleInDatacenterOrRegion(dcName, itemId, false);
                var recentPurchaseRegionNq = await _saleStore.GetMostRecentSaleInDatacenterOrRegion(regionName, itemId, false);
                var recentPurchaseWorldHq = await _saleStore.GetMostRecentSaleInWorld(worldId, itemId, true);
                var recentPurchaseDcHq = await _saleStore.GetMostRecentSaleInDatacenterOrRegion(dcName, itemId, true);
                var recentPurchaseRegionHq = await _saleStore.GetMostRecentSaleInDatacenterOrRegion(regionName, itemId, true);
                var worldVelocity = await _saleStore.RetrieveUnitTradeVelocity(worldId.ToString(), itemId, tradeVelocityCalculationRange, today, cts.Token);
                var dcVelocity = await _saleStore.RetrieveUnitTradeVelocity(dcName, itemId, tradeVelocityCalculationRange, today, cts.Token);
                var regionVelocity = await _saleStore.RetrieveUnitTradeVelocity(regionName, itemId, tradeVelocityCalculationRange, today, cts.Token);

                var nq = new AggregatedMarketBoardData.AggregatedResult(
                    GetMinListing(minListing, e => e?.Nq),
                    null,
                    RecentPurchase(recentPurchaseWorldNq, recentPurchaseDcNq, recentPurchaseRegionNq),
                    GetAverageSalePrice(worldVelocity.Nq, dcVelocity.Nq, regionVelocity.Nq),
                    GetDailySaleVelocity(worldVelocity.Nq, dcVelocity.Nq, regionVelocity.Nq));

                var hq = new AggregatedMarketBoardData.AggregatedResult(
                    GetMinListing(minListing, e => e?.Hq),
                    null,
                    RecentPurchase(recentPurchaseWorldHq, recentPurchaseDcHq, recentPurchaseRegionHq),
                    GetAverageSalePrice(worldVelocity.Hq, dcVelocity.Hq, regionVelocity.Hq),
                    GetDailySaleVelocity(worldVelocity.Hq, dcVelocity.Hq, regionVelocity.Hq));

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
            recentPurchaseWorld != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(recentPurchaseWorld.PricePerUnit, new DateTimeOffset(recentPurchaseWorld.SaleTime).ToUnixTimeMilliseconds(), null) : null,
            recentPurchaseDc != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(recentPurchaseDc.PricePerUnit, new DateTimeOffset(recentPurchaseDc.SaleTime).ToUnixTimeMilliseconds(), recentPurchaseDc.WorldId) : null,
            recentPurchaseRegion != null ? new AggregatedMarketBoardData.RecentPurchase.Entry(recentPurchaseRegion.PricePerUnit, new DateTimeOffset(recentPurchaseRegion.SaleTime).ToUnixTimeMilliseconds(), recentPurchaseRegion.WorldId) : null);
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