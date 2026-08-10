using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
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
    private readonly IUploadLogDbAccess _uploadLogDb;
    private readonly IGameDataProvider _gdp;
    private readonly IBus _bus;
    private readonly ILogger<MarketBoardUploadBehavior> _logger;

    public MarketBoardUploadBehavior(
        ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess historyDb,
        IUploadLogDbAccess uploadLogDb,
        IGameDataProvider gdp,
        IBus bus,
        ILogger<MarketBoardUploadBehavior> logger)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
        _uploadLogDb = uploadLogDb;
        _gdp = gdp;
        _bus = bus;
        _logger = logger;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        var cond = parameters.WorldId != null;
        cond &= parameters.ItemId != null;
        cond &= parameters.Sales != null || parameters.Listings != null;

        if (cond)
        {
            var stackSize = _gdp.MarketableItemStackSizes()[parameters.ItemId.Value];

            // Validate entries; .All returns false if the list is empty, so check that first
            // TODO: Reject uploads with bad data instead of just filtering the bad data out after Dalamud fixes sales
            if (parameters.Sales?.Count > 0)
            {
                cond &= parameters.Sales.All(s => s.Quantity <= stackSize && s.PricePerUnit is <= 999_999_999);
                parameters.Sales = parameters.Sales.Where(s => s.Quantity is > 0).ToList();
            }

            if (parameters.Listings?.Count > 0)
            {
                cond &= parameters.Listings.All(s => s.Quantity <= stackSize && s.PricePerUnit is <= 999_999_999);
                parameters.Listings = parameters.Listings.Where(l => l.Quantity is > 0).ToList();
            }
        }

        return cond;
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketBoardUploadBehavior.Execute");

        // ReSharper disable PossibleInvalidOperationException
        var worldId = parameters.WorldId.Value;
        var itemId = parameters.ItemId.Value;
        // ReSharper restore PossibleInvalidOperationException

        // Add world/item to traces
        activity?.AddTag("worldId", worldId);
        activity?.AddTag("itemId", itemId);

        var uploadedListingsCount = parameters.Listings?.Count ?? 0;
        var uploadedSalesCount = parameters.Sales?.Count ?? 0;
        var userAgent = NormalizeUserAgent(parameters.UserAgent);

        await LogUploadEvent("MarketBoardUpload", source, worldId, itemId, uploadedListingsCount, uploadedSalesCount, userAgent);

        if (parameters.Sales != null)
        {
            if (parameters.Sales.Any(s =>
                    Util.HasHtmlTags(s.BuyerName) || Util.HasHtmlTags(s.SellerId) || Util.HasHtmlTags(s.BuyerId)))
            {
                await LogUploadEvent("SalesUploadMalformed", source, worldId, itemId, uploadedListingsCount, uploadedSalesCount, userAgent);
                return new BadRequestResult();
            }

            var addedSalesCount = await HandleSales(parameters.Sales, itemId, worldId, parameters.UploaderId, cancellationToken);
            await LogUploadEvent("SalesUploadSuccess", source, worldId, itemId, uploadedListingsCount, addedSalesCount, userAgent);
        }

        if (parameters.Listings != null)
        {
            if (parameters.Listings.Any(IsInvalid))
            {
                await LogUploadEvent("ListingsUploadMalformed", source, worldId, itemId, uploadedListingsCount, uploadedSalesCount, userAgent);
                return new BadRequestResult();
            }

            if (!string.IsNullOrEmpty(parameters.UploaderRetainerId) &&
                parameters.Listings.Any(l => l.RetainerId == parameters.UploaderRetainerId))
            {
                await LogUploadEvent("ListingsUploadMalformed", source, worldId, itemId, uploadedListingsCount, uploadedSalesCount, userAgent);
                return new BadRequestResult();
            }

            var newListingsCount = await HandleListings(parameters.Listings, itemId, worldId, source, parameters.UploaderRetainerId, cancellationToken);
            await LogUploadEvent("ListingsUploadSuccess", source, worldId, itemId, newListingsCount, uploadedSalesCount, userAgent);
        }

        return null;
    }

    private static string NormalizeUserAgent(string userAgent)
    {
        return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
    }

    private Task LogUploadEvent(string @event, ApiKey source, int worldId, int itemId, int listings, int sales, string userAgent)
    {
        // Version-7 Guids embed a millisecond timestamp in the high bits, so
        // batch inserts hit sequential B-tree leaf pages instead of random
        // ones - the same write-amplification problem that motivated this
        // feature's earlier disablement in PR #1302.
        return _uploadLogDb.LogAction(new UploadLogEntry
        {
            Id = Guid.CreateVersion7(),
            Timestamp = DateTime.UtcNow,
            Event = @event,
            Application = source.Name,
            WorldId = worldId,
            ItemId = itemId,
            Listings = listings,
            Sales = sales,
            UserAgent = userAgent,
        });
    }

    private static bool IsInvalid(Schema.Listing l)
    {
        return Util.HasHtmlTags(l.ListingId) || Util.HasHtmlTags(l.RetainerName) ||
               Util.HasHtmlTags(l.RetainerId) || Util.HasHtmlTags(l.CreatorName) || Util.HasHtmlTags(l.SellerId) ||
               Util.HasHtmlTags(l.CreatorId);
    }

    private async Task<int> HandleListings(IList<Schema.Listing> uploadedListings, int itemId, int worldId,
        ApiKey source, string uploaderRetainerId = null, CancellationToken cancellationToken = default)
    {
        var newListings = CleanUploadedListings(uploadedListings, itemId, worldId, source.Name);
        var uploadedCount = newListings.Count;

        // What this upload says the board holds. Kept as our own copy because the
        // document's list belongs to the storage layer once handed over, and a
        // retention write merges the surviving rows into it - which would otherwise
        // read back as listings this upload had just added.
        var uploaded = newListings.ToList();

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var document = new CurrentlyShown
        {
            WorldId = worldId,
            ItemId = itemId,
            LastUploadTimeUnixMilliseconds = now,
            UploadSource = source.Name,
            Listings = newListings,
        };

        // The write returns the rows it displaced, so the pre-upload board comes back
        // from the statement that replaced it. No second read, and nothing to order
        // against the write.
        var replacedListings = await _currentlyShownDb.Update(document, new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, uploaderRetainerId, cancellationToken);

        _ = PublishListingsToMessageBus(replacedListings, uploaded, worldId, itemId, cancellationToken);

        return uploadedCount;
    }

    /// <summary>
    /// Publishes the add/remove diff for an upload.
    /// </summary>
    /// <remarks>
    /// <paramref name="replacedListings"/> is what the write actually displaced,
    /// not the result of a second read. Reading the prior board separately cannot
    /// be ordered against the write that destroys it: when that read lost the race
    /// the "old" listings were the ones just written, both diffs came out empty,
    /// and a real change was published as no frame at all.
    ///
    /// It also means retention needs no filtering here. The scoped delete leaves
    /// the uploader's retainer's rows in place, so they are absent from what was
    /// displaced and cannot be mistaken for removals - decided by what the database
    /// did rather than by re-deriving it.
    /// </remarks>
    private async Task PublishListingsToMessageBus(IList<Listing> replacedListings, IList<Listing> listings,
        int worldId, int itemId, CancellationToken cancellationToken = default)
    {
        if (_bus == null) return;

        var addedListings = listings.Where(l => !replacedListings.Contains(l)).ToList();
        var removedListings = replacedListings.Where(l => !listings.Contains(l)).ToList();

        if (removedListings.Count > 0)
        {
            try
            {
                await _bus.Publish(new ListingsRemove
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = removedListings
                        .Select(Util.ListingToView)
                        .ToList(),
                }, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish ListingsRemove event");
            }
        }

        if (addedListings.Count > 0)
        {
            try
            {
                await _bus.Publish(new ListingsAdd
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = addedListings
                        .Select(Util.ListingToView)
                        .ToList(),
                }, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish ListingsAdd event");
            }
        }
    }

    private async Task<int> HandleSales(IList<Schema.Sale> uploadedSales, int itemId, int worldId, string uploaderId,
        CancellationToken cancellationToken = default)
    {
        var cleanSales = CleanUploadedSales(uploadedSales, worldId, itemId, uploaderId);

        var addedSales = new List<Sale>();

        var existingHistory = await _historyDb.Retrieve(new HistoryQuery
        {
            WorldId = worldId,
            ItemId = itemId,
            Count = uploadedSales?.Count ?? 0,
        }, cancellationToken);

        if (existingHistory == null)
        {
            addedSales.AddRange(cleanSales);
            await _historyDb.Create(new History
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Sales = cleanSales,
            }, cancellationToken);
        }
        else
        {
            // Remove duplicates
            addedSales.AddRange(cleanSales.Where(t => !existingHistory.Sales.Contains(t)));
            await _historyDb.InsertSales(addedSales, new HistoryQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            }, cancellationToken);
        }

        _ = PublishSalesToMessageBus(addedSales, itemId, worldId, cancellationToken);

        return addedSales.Count;
    }

    private async Task PublishSalesToMessageBus(IList<Sale> sales, int itemId, int worldId,
        CancellationToken cancellationToken = default)
    {
        if (_bus != null && sales.Count > 0)
        {
            try
            {
                await _bus.Publish(new SalesAdd
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Sales = sales.Select(Util.SaleToView).ToList(),
                }, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to publish SalesAdd event");
            }
        }
    }

    private static List<Listing> CleanUploadedListings(IEnumerable<Schema.Listing> uploadedListings, int itemId,
        int worldId, string sourceName)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketBoardUploadBehavior.CleanUploadedListings");

        return uploadedListings
            .Select(l =>
            {
                // Listing IDs from some uploaders are empty; this needs to be fixed
                // but this should be a decent workaround that still enables data
                // collection.
                var listingId = l.ListingId;
                if (string.IsNullOrEmpty(listingId))
                {
                    using var sha256 = SHA256.Create();
                    var hashString =
                        $"{l.CreatorId}:{l.CreatorName}:${l.RetainerName}:${l.RetainerId}:${l.SellerId}:${l.LastReviewTimeUnixSeconds}:${l.Quantity}:${l.PricePerUnit}:${string.Join(',', l.Materia)}:${itemId}:${worldId}";
                    listingId = $"dirty:{Util.Hash(sha256, hashString)}";
                }

                return new Listing
                {
                    ListingId = listingId,
                    ItemId = itemId,
                    WorldId = worldId,
                    Hq = Util.ParseUnusualBool(l.Hq),
                    OnMannequin = Util.ParseUnusualBool(l.OnMannequin),
                    Materia = l.Materia?
                        .Where(s => s.SlotId != null && s.MateriaId != null)
                        .Select(s => new Materia
                        {
                            SlotId = (int)s.SlotId!,
                            MateriaId = (int)s.MateriaId!,
                        })
                        .ToList() ?? new List<Materia>(),
                    PricePerUnit = l.PricePerUnit ?? 0,
                    Quantity = l.Quantity ?? 0,
                    DyeId = l.DyeId ?? 0,
                    CreatorId = Util.ParseUnusualId(l.CreatorId) ?? "",
                    CreatorName = l.CreatorName,
                    LastReviewTime = GetLastReviewTime(l),
                    RetainerId = Util.ParseUnusualId(l.RetainerId) ?? "",
                    RetainerName = l.RetainerName,
                    RetainerCityId = l.RetainerCityId ?? 0,
                    SellerId = Util.ParseUnusualId(l.SellerId) ?? "",
                    Source = sourceName,
                };
            })
            .Where(l => l.PricePerUnit > 0)
            .Where(l => l.Quantity > 0)
            .DistinctBy(l => l.ListingId)
            .OrderBy(l => l.PricePerUnit)
            .ToList();
    }

    private static DateTime GetLastReviewTime(Schema.Listing l)
    {
        var lastReviewTimeSeconds = l.LastReviewTimeUnixSeconds ?? 0;
        return lastReviewTimeSeconds == 0
            ? DateTime.UtcNow
            : DateTimeOffset.FromUnixTimeSeconds(lastReviewTimeSeconds).UtcDateTime;
    }

    private static List<Sale> CleanUploadedSales(IEnumerable<Schema.Sale> uploadedSales, int worldId, int itemId,
        string uploaderIdSha256)
    {
        using var activity = Util.ActivitySource.StartActivity("MarketBoardUploadBehavior.CleanUploadedSales");

        return uploadedSales
            .Where(s => s.TimestampUnixSeconds > 0)
            .Select(s => new Sale
            {
                Id = Guid.NewGuid(),
                WorldId = worldId,
                ItemId = itemId,
                Hq = Util.ParseUnusualBool(s.Hq),
                BuyerName = s.BuyerName,
                OnMannequin = Util.ParseUnusualBool(s.OnMannequin),
                PricePerUnit = s.PricePerUnit ?? 0,
                Quantity = s.Quantity ?? 0,
                SaleTime = DateTimeOffset.FromUnixTimeSeconds(s.TimestampUnixSeconds ?? 0).UtcDateTime,
                UploaderIdHash = uploaderIdSha256,
            })
            .Where(s => s.PricePerUnit > 0)
            .Where(s => s.Quantity > 0)
            .Where(s => new DateTimeOffset(s.SaleTime).ToUnixTimeSeconds() > 0)
            .OrderByDescending(s => s.SaleTime)
            .ToList();
    }
}
