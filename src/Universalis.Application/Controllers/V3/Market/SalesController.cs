using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V3.Market;
using Universalis.DbAccess.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V3.Market;

[ApiController]
[ApiVersion("3")]
[Route("api/v{version:apiVersion}/market/sales/{servers}/{itemId}")]
public class SalesController : ControllerBase
{
    private const int SalesPerPage = 100;

    protected readonly IGameDataProvider GameData;
    protected readonly ISaleStore Store;

    public SalesController(
        IGameDataProvider gameData,
        ISaleStore store)
    {
        GameData = gameData;
        Store = store;
    }

    /// <summary>
    /// Retrieve the sales for the provided item on the requested servers.
    /// </summary>
    /// <param name="servers">A comma-separated list of servers to search.</param>
    /// <param name="itemId">The ID of the item to look up.</param>
    /// <param name="cursor">A cursor into the desired page of sales.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">A world/DC or the item requested is invalid.</response>
    [ApiTag("Market board sales")]
    [ProducesResponseType(typeof(SalesPage), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SalesPage>> Get(Servers servers, int itemId,
        [FromQuery(Name = "cursor")] string cursor,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SalesControllerV3.Get");
        activity?.AddTag("itemId", itemId);

        if (!servers.TryResolveWorlds(GameData, out var worlds))
        {
            return NotFound();
        }

        if (!SalesCursor.TryParse(cursor, out var salesCursor))
        {
            salesCursor = SalesCursor.Create();
        }

        var sales = await worlds.ToAsyncEnumerable()
            .SelectManyAwaitWithCancellation(async (world, ct) =>
            {
                using var worldDataActivity = Util.ActivitySource.StartActivity("SalesControllerV3.Get.WorldData");
                worldDataActivity?.AddTag("itemId", itemId);
                worldDataActivity?.AddTag("worldId", world.Id); 

                var data = await Store.RetrieveBySaleTime(world.Id, itemId, SalesPerPage, salesCursor.From,
                    ct);
                return data.ToAsyncEnumerable()
                    .Select(sale => ToSaleView(world, sale));
            })
            .OrderByDescending(sale => sale.TimestampUnixMilliseconds)
            .ToListAsync(cancellationToken);

        return new ActionResult<SalesPage>(new SalesPage
        {
            Sales = sales,
            Next = SalesCursor.FromUnixMilliseconds(sales.Last().TimestampUnixMilliseconds - 1).ToString(),
        });
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