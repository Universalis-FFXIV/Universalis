using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V3.Market;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V3.Market;

[ApiController]
[ApiVersion("3")]
[Route("api/v{version:apiVersion}/market/overview/{servers}/{itemId}")]
public class OverviewController : ControllerBase
{
    protected readonly IGameDataProvider GameData;
    protected readonly ICurrentlyShownDbAccess CurrentlyShown;
    protected readonly IHistoryDbAccess History;

    public OverviewController(
        IGameDataProvider gameData,
        ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess history)
    {
        GameData = gameData;
        CurrentlyShown = currentlyShownDb;
        History = history;
    }

    /// <summary>
    /// Retrieves an overview of the current market data for the provided item and servers. This contains
    /// the current listings and a limited number of sales.
    /// </summary>
    /// <param name="servers">A comma-separated list of servers to search.</param>
    /// <param name="itemId">The ID of the item to look up.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">A world/DC or the item requested is invalid.</response>
    [ApiTag("Market board overview")]
    [ProducesResponseType(typeof(MarketOverview), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MarketOverview>> Get(Servers servers, int itemId,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("OverviewControllerV3.Get");
        activity?.AddTag("itemId", itemId);

        if (!servers.TryResolveWorlds(GameData, out var worlds))
        {
            return NotFound();
        }

        var sales = new List<Sale>();
        var listings = new List<Listing>();
        var uploadTimes = new Dictionary<int, long?>();
        foreach (var world in worlds)
        {
            using var worldDataActivity = Util.ActivitySource.StartActivity("OverviewControllerV3.Get.WorldData");
            worldDataActivity?.AddTag("itemId", itemId);
            worldDataActivity?.AddTag("worldId", world.Id);

            var currentData = await CurrentlyShown.Retrieve(
                new CurrentlyShownQuery { ItemId = itemId, WorldId = world.Id },
                cancellationToken);
            var history = await History.Retrieve(new HistoryQuery { ItemId = itemId, WorldId = world.Id, Count = 20 },
                cancellationToken);

            if (currentData != null)
            {                
                uploadTimes[world.Id] = currentData.LastUploadTimeUnixMilliseconds;
                listings.AddRange(currentData.Listings.Select(listing => ToListingView(world, listing)));
            }

            if (history != null)
            {
                uploadTimes[world.Id] = Convert.ToInt64(history.LastUploadTimeUnixMilliseconds);
                sales.AddRange(history.Sales.Select(sale => ToSaleView(world, sale)));
            }
        }

        return new ActionResult<MarketOverview>(new MarketOverview
        {
            ItemId = itemId,
            LastUpdateTimeUnixMilliseconds = uploadTimes,
            Listings = listings,
            Sales = sales,
        });
    }

    private static Listing ToListingView(World world, Universalis.Entities.MarketBoard.Listing listing)
    {
        var total = (int)Math.Ceiling(listing.PricePerUnit * listing.Quantity * 1.05);
        return new Listing
        {
            ListingIdHash = listing.ListingId,
            World = world.Id,
            LastReviewTimeUnixMilliseconds = new DateTimeOffset(listing.LastReviewTime).ToUnixTimeMilliseconds(),
            PricePerUnit = total / Convert.ToDecimal(listing.Quantity),
            Quantity = listing.Quantity,
            Total = total,
            Hq = listing.Hq,
            DyeId = listing.DyeId,
            Creator = !string.IsNullOrEmpty(listing.CreatorName)
                ? new Creator { IdHash = listing.CreatorId, Name = listing.CreatorName }
                : null,
            Materia = new List<int>(),
            OnMannequin = listing.OnMannequin,
            Retainer = new Retainer { IdHash = listing.RetainerId, Name = listing.RetainerName },
            Seller = new Seller { IdHash = listing.SellerId },
        };
    }

    private static Sale ToSaleView(World world, Universalis.Entities.MarketBoard.Sale sale)
    {
        return new Sale
        {
            World = world.Id,
            PricePerUnit = sale.PricePerUnit,
            Quantity = sale.Quantity,
            Hq = sale.Hq,
            TimestampUnixMilliseconds = new DateTimeOffset(sale.SaleTime).ToUnixTimeMilliseconds(),
            OnMannequin = sale.OnMannequin,
            BuyerName = sale.BuyerName,
        };
    }
}