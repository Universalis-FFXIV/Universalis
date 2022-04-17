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
using Universalis.Application.Realtime.Message;
using Universalis.Application.Uploads.Schema;
using Universalis.Application.Views.V1;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;
using Listing = Universalis.Entities.MarketBoard.Listing;
using Materia = Universalis.Entities.Materia;
using Sale = Universalis.Entities.MarketBoard.Sale;

namespace Universalis.Application.Uploads.Behaviors;

public class MarketBoardUploadBehavior : IUploadBehavior
{
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IHistoryDbAccess _historyDb;
    private readonly ICache<CurrentlyShownQuery, CurrentlyShownView> _cache;
    private readonly ISocketProcessor _sockets;

    private static readonly Counter CacheDeletes = Metrics.CreateCounter("universalis_cache_deletes", "Cache Deletes");

    public MarketBoardUploadBehavior(
        ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess historyDb,
        ICache<CurrentlyShownQuery, CurrentlyShownView> cache,
        ISocketProcessor sockets)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
        _cache = cache;
        _sockets = sockets;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return parameters.WorldId != null
               && parameters.ItemId != null
               && (parameters.Sales != null || parameters.Listings != null);
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

            cleanSales = await parameters.Sales
                .ToAsyncEnumerable()
                .Select(s => new Sale
                {
                    Hq = Util.ParseUnusualBool(s.Hq),
                    BuyerName = s.BuyerName,
                    PricePerUnit = s.PricePerUnit ?? 0,
                    Quantity = s.Quantity ?? 0,
                    TimestampUnixSeconds = s.TimestampUnixSeconds ?? 0,
                    UploadApplicationName = source.Name,
                })
                .Where(s => s.PricePerUnit > 0)
                .Where(s => s.Quantity > 0)
                .Where(s => s.TimestampUnixSeconds > 0)
                .OrderByDescending(s => s.TimestampUnixSeconds)
                .ToListAsync(cancellationToken);

            var existingHistory = await _historyDb.Retrieve(new HistoryQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            }, cancellationToken);
            var minimizedSales = await cleanSales
                .ToAsyncEnumerable()
                .Select(s => MinimizedSale.FromSale(s, parameters.UploaderId))
                .ToListAsync(cancellationToken);

            var historyDocument = new History
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(), // TODO: Make this not risk overflowing
            };

            if (existingHistory == null)
            {
                historyDocument.Sales = minimizedSales;
                await _historyDb.Create(historyDocument, cancellationToken);
            }
            else
            {
                // Remove duplicates
                var head = existingHistory.Sales.FirstOrDefault();
                for (var i = 0; i < minimizedSales.Count; i++)
                {
                    if (minimizedSales[i].Equals(head))
                    {
                        break;
                    }

                    existingHistory.Sales.Insert(0, minimizedSales[i]);
                }

                // Trims out duplicates and any invalid data
                existingHistory.Sales = await existingHistory.Sales
                    .ToAsyncEnumerable()
                    .Where(s => s.PricePerUnit > 0) // We check PPU and *not* quantity because there are entries from before quantity was tracked
                    .Distinct()
                    .OrderByDescending(s => s.SaleTimeUnixSeconds)
                    .ToListAsync(cancellationToken);

                historyDocument.Sales = existingHistory.Sales;
                await _historyDb.Update(historyDocument, new HistoryQuery
                {
                    WorldId = worldId,
                    ItemId = itemId,
                }, cancellationToken);
            }
        }

        List<Listing> cleanListings = null;
        if (parameters.Listings != null)
        {
            if (Util.HasHtmlTags(JsonSerializer.Serialize(parameters.Listings)))
            {
                return new BadRequestResult();
            }

            cleanListings = await parameters.Listings
                .ToAsyncEnumerable()
                .Select(l =>
                {
                    return new Listing
                    {
                        ListingIdInternal = l.ListingId,
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
                        CreatorIdInternal = Util.ParseUnusualId(l.CreatorId),
                        CreatorName = l.CreatorName,
                        LastReviewTimeUnixSeconds = l.LastReviewTimeUnixSeconds ?? 0,
                        RetainerIdInternal = Util.ParseUnusualId(l.RetainerId),
                        RetainerName = l.RetainerName,
                        RetainerCityIdInternal = l.RetainerCityId,
                        SellerIdInternal = Util.ParseUnusualId(l.SellerId),
                        UploadApplicationName = source.Name,
                    };
                })
                .Where(l => l.PricePerUnit > 0)
                .Where(l => l.Quantity > 0)
                .OrderBy(l => l.PricePerUnit)
                .ToListAsync(cancellationToken);
        }

        var existingCurrentlyShown = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);

        var document = new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            Listings = cleanListings ?? existingCurrentlyShown?.Listings ?? new List<Listing>(),
            RecentHistory = cleanSales ?? existingCurrentlyShown?.RecentHistory ?? new List<Sale>(),
            UploaderIdHash = parameters.UploaderId,
        };

        if (_cache.Delete(new CurrentlyShownQuery { ItemId = itemId, WorldId = worldId }))
        {
            CacheDeletes.Inc();
        }

        await _currentlyShownDb.Update(document, new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);

        _sockets.BroadcastUpdate(new ItemUpdate
        {
            WorldId = worldId,
            ItemId = itemId,
        });

        return null;
    }
}