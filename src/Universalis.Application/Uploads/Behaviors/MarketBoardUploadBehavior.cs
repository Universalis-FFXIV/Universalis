using Microsoft.AspNetCore.Mvc;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;
using Universalis.GameData;
using Listing = Universalis.Entities.MarketBoard.Listing;
using Materia = Universalis.Entities.Materia;
using Sale = Universalis.Entities.MarketBoard.Sale;

namespace Universalis.Application.Uploads.Behaviors;

public class MarketBoardUploadBehavior : IUploadBehavior
{
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IHistoryDbAccess _historyDb;
    private readonly ICache<CurrentlyShownQuery, CachedCurrentlyShownData> _cache;
    private readonly ISocketProcessor _sockets;
    private readonly IGameDataProvider _gdp;

    private static readonly Counter CacheDeletes = Metrics.CreateCounter("universalis_cache_deletes", "Cache Deletes");

    public MarketBoardUploadBehavior(
        ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess historyDb,
        ICache<CurrentlyShownQuery, CachedCurrentlyShownData> cache,
        ISocketProcessor sockets,
        IGameDataProvider gdp)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
        _cache = cache;
        _sockets = sockets;
        _gdp = gdp;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        var cond = parameters.WorldId != null;
        cond &= parameters.ItemId != null;
        cond &= parameters.Sales != null || parameters.Listings != null;
        
        if (cond)
        {
            var stackSize = _gdp.MarketableItemStackSizes()[parameters.ItemId.Value];
            
            if (parameters.Sales != null)
            {
                cond &= parameters.Sales.All(l => l.Quantity is > 0 && l.Quantity <= stackSize);
            }

            if (parameters.Listings != null)
            {
                cond &= parameters.Listings.All(l => l.Quantity is > 0 && l.Quantity <= stackSize);
            }
        }
        
        return cond;
    }

    public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        // ReSharper disable PossibleInvalidOperationException
        var worldId = parameters.WorldId.Value;
        var itemId = parameters.ItemId.Value;
        // ReSharper restore PossibleInvalidOperationException

        List<Sale> cleanSales = null;
        if (parameters.Sales != null)
        {
            if (Util.HasHtmlTags(JsonSerializer.Serialize(parameters.Sales)))
            {
                return new BadRequestResult();
            }

            cleanSales = parameters.Sales
                .Select(s => new Sale
                {
                    Hq = Util.ParseUnusualBool(s.Hq),
                    BuyerName = s.BuyerName,
                    PricePerUnit = s.PricePerUnit ?? 0,
                    Quantity = s.Quantity ?? 0,
                    TimestampUnixSeconds = s.TimestampUnixSeconds ?? 0,
                    UploaderIdHash = parameters.UploaderId
                })
                .Where(s => s.PricePerUnit > 0)
                .Where(s => s.Quantity > 0)
                .Where(s => s.TimestampUnixSeconds > 0)
                .OrderByDescending(s => s.TimestampUnixSeconds)
                .ToList();

            var existingHistory = await _historyDb.Retrieve(new HistoryQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            }, cancellationToken);

            // Used for WebSocket updates
            var addedSales = new List<Sale>();

            var historyDocument = new History
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            };

            if (existingHistory == null)
            {
                historyDocument.Sales = cleanSales;
                await _historyDb.Create(historyDocument, cancellationToken);
            }
            else
            {
                // Remove duplicates
                var head = existingHistory.Sales.FirstOrDefault();
                foreach (var sale in cleanSales.TakeWhile(t => !t.Equals(head)))
                {
                    existingHistory.Sales.Insert(0, sale);
                    addedSales.Add(sale);
                }

                // Trims out duplicates and any invalid data
                existingHistory.Sales = existingHistory.Sales
                    .Where(s => s.PricePerUnit > 0) // We check PPU and *not* quantity because there are entries from before quantity was tracked
                    .Distinct()
                    .OrderByDescending(s => s.TimestampUnixSeconds)
                    .ToList();

                historyDocument.Sales = existingHistory.Sales;
                await _historyDb.Update(historyDocument, new HistoryQuery
                {
                    WorldId = worldId,
                    ItemId = itemId,
                }, cancellationToken);
            }

            if (addedSales.Count > 0)
            {
                await Task.Run(() =>
                {
                    _sockets.Publish(new SalesAdd
                    {
                        WorldId = worldId,
                        ItemId = itemId,
                        Sales = addedSales.Select(Util.SaleSimpleToView).ToList(),
                    });
                }, cancellationToken);
            }
        }

        var existingCurrentlyShown = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);

        List<Listing> newListings = null;
        if (parameters.Listings != null)
        {
            if (Util.HasHtmlTags(JsonSerializer.Serialize(parameters.Listings)))
            {
                return new BadRequestResult();
            }

            newListings = parameters.Listings
                .Select(l =>
                {
                    return new Listing
                    {
                        ListingId = l.ListingId ?? "",
                        Hq = Util.ParseUnusualBool(l.Hq),
                        OnMannequin = Util.ParseUnusualBool(l.OnMannequin),
                        Materia = l.Materia?
                            .Where(s => s.SlotId != null && s.MateriaId != null)
                            .Select(s => new Materia
                            {
                                SlotId = (uint)s.SlotId!,
                                MateriaId = (uint)s.MateriaId!,
                            })
                            .ToList() ?? new List<Materia>(),
                        PricePerUnit = l.PricePerUnit ?? 0,
                        Quantity = l.Quantity ?? 0,
                        DyeId = l.DyeId ?? 0,
                        CreatorId = Util.ParseUnusualId(l.CreatorId) ?? "",
                        CreatorName = l.CreatorName,
                        LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds ?? 0,
                        RetainerId = Util.ParseUnusualId(l.RetainerId) ?? "",
                        RetainerName = l.RetainerName,
                        RetainerCityId = l.RetainerCityId ?? 1, // TODO: This probably shouldn't have a default value
                        SellerId = Util.ParseUnusualId(l.SellerId) ?? "",
                    };
                })
                .Where(l => l.PricePerUnit > 0)
                .Where(l => l.Quantity > 0)
                .OrderBy(l => l.PricePerUnit)
                .ToList();

            var oldListings = existingCurrentlyShown?.Listings ?? new List<Listing>();
            var addedListings = newListings.Where(l => !oldListings.Contains(l)).ToList();
            var removedListings = oldListings.Where(l => !newListings.Contains(l)).ToList();

            if (addedListings.Count > 0)
            {
                await Task.Run(async () =>
                {
                    _sockets.Publish(new ListingsAdd
                    {
                        WorldId = worldId,
                        ItemId = itemId,
                        Listings = await addedListings
                            .ToAsyncEnumerable()
                            .SelectAwait(async l => await Util.ListingSimpleToView(l, cancellationToken))
                            .ToListAsync(cancellationToken),
                    });
                }, cancellationToken);
            }

            if (removedListings.Count > 0)
            {
                await Task.Run(async () =>
                {
                    _sockets.Publish(new ListingsRemove
                    {
                        WorldId = worldId,
                        ItemId = itemId,
                        Listings = await removedListings
                            .ToAsyncEnumerable()
                            .SelectAwait(async l => await Util.ListingSimpleToView(l, cancellationToken))
                            .ToListAsync(cancellationToken),
                    });
                }, cancellationToken);
            }
        }

        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var listings = newListings ?? existingCurrentlyShown?.Listings ?? new List<Listing>();
        var sales = cleanSales ?? existingCurrentlyShown?.Sales ?? new List<Sale>();
        var document = new CurrentlyShown(worldId, itemId, now, source.Name, listings, sales);

        if (await _cache.Delete(new CurrentlyShownQuery { ItemId = itemId, WorldId = worldId }, cancellationToken))
        {
            CacheDeletes.Inc();
        }

        await _currentlyShownDb.Update(document, new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);
        
        return null;
    }
}