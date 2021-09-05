using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Common;
using Universalis.Application.Views;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/{worldOrDc}/{itemIds}/cheapest")]
    public class CheapestListingController : WorldDcControllerBase
    {
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;

        protected CheapestListingController(IGameDataProvider gameData, ICurrentlyShownDbAccess currentlyShownDb) :
            base(gameData)
        {
            _currentlyShownDb = currentlyShownDb;
        }

        /// <summary>
        /// Retrieves the current cheapest listing for each of the requested items.
        /// </summary>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Data retrieved successfully.</response>
        /// <response code="404">
        /// The world/DC or item requested is invalid. When requesting multiple items at once, an invalid item ID
        /// will not trigger this. Instead, the returned data will simply contain null under the
        /// invalid item IDs.
        /// </response>
        [HttpGet]
        [ProducesResponseType(typeof(CheapestView), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(string worldOrDc, string itemIds, CancellationToken cancellationToken = default)
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
                if (!GameData.MarketableItemIds().Contains(itemIdsArray[0]))
                {
                    return NotFound();
                }
            }

            return Ok(new CheapestView
            {
                Items = new SortedDictionary<uint, ListingView>(await itemIdsArray
                    .ToAsyncEnumerable()
                    .SelectAwait(async itemId => new
                    {
                        ItemId = itemId,
                        Listing = await GetCheapestListing(itemId, worldDc, worldIds, cancellationToken),
                    })
                    .ToDictionaryAsync(l => l.ItemId, l => l.Listing, cancellationToken)),
            });
        }

        private async Task<ListingView> GetCheapestListing(uint itemId, WorldDc worldDc, uint[] worldIds, CancellationToken cancellationToken = default)
        {
            var data =
                await _currentlyShownDb.RetrieveMany(new CurrentlyShownManyQuery { ItemId = itemId, WorldIds = worldIds },
                    cancellationToken);
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
                            var listingView = await ListingView.FromListing(l, cancellationToken);
                            listingView.WorldId = worldDc.IsDc ? next.WorldId : null;
                            listingView.WorldName = worldDc.IsDc ? worlds[next.WorldId] : null;
                            return listingView;
                        })
                        .ToListAsync(cancellationToken);

                    return agg;
                }, cancellationToken);
            currentlyShown.Listings.Sort((a, b) => (int)a.PricePerUnit - (int)b.PricePerUnit);
            return currentlyShown.Listings.FirstOrDefault();
        }
    }
}