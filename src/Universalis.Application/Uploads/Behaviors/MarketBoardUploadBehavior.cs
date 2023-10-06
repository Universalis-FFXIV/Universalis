using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
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

    public MarketBoardUploadBehavior(
        ICurrentlyShownDbAccess currentlyShownDb,
        IHistoryDbAccess historyDb,
        IUploadLogDbAccess uploadLogDb,
        IGameDataProvider gdp,
        IBus bus)
    {
        _currentlyShownDb = currentlyShownDb;
        _historyDb = historyDb;
        _uploadLogDb = uploadLogDb;
        _gdp = gdp;
        _bus = bus;
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
                cond &= parameters.Sales.All(s =>
                    s.Quantity is > 0 && s.Quantity <= stackSize && s.PricePerUnit is <= 999_999_999);
            }

            if (parameters.Listings != null)
            {
                cond &= parameters.Listings.All(l =>
                    l.Quantity is > 0 && l.Quantity <= stackSize && l.PricePerUnit is <= 999_999_999);
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

        // Log the upload
        await _uploadLogDb.LogAction(new UploadLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Event = "MarketBoardUpload",
            Application = source.Name,
            WorldId = worldId,
            ItemId = itemId,
            Listings = parameters.Listings?.Count ?? -1,
            Sales = parameters.Sales?.Count ?? -1,
        });

        // Most uploads have both sales and listings
        var currentlyShownTask = _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldId,
            ItemId = itemId,
        }, cancellationToken);
        var existingHistoryTask = _historyDb.Retrieve(new HistoryQuery
        {
            WorldId = worldId,
            ItemId = itemId,
            Count = parameters.Sales?.Count ?? 0,
        }, cancellationToken);

        await Task.WhenAll(currentlyShownTask, existingHistoryTask);
        var existingCurrentlyShown = await currentlyShownTask;
        var existingHistory = await existingHistoryTask;

        if (parameters.Sales != null)
        {
            if (parameters.Sales.Any(s =>
                    Util.HasHtmlTags(s.BuyerName) || Util.HasHtmlTags(s.SellerId) || Util.HasHtmlTags(s.BuyerId)))
            {
                await _uploadLogDb.LogAction(new UploadLogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Event = "SalesUploadMalformed",
                    Application = source.Name,
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = parameters.Listings?.Count ?? -1,
                    Sales = parameters.Sales?.Count ?? -1,
                });

                return new BadRequestResult();
            }

            var cleanSales = CleanUploadedSales(parameters.Sales, worldId, itemId, parameters.UploaderId);

            var addedSales = new List<Sale>();

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

            if (addedSales.Count > 0)
            {
                _ = _bus?.Publish(new SalesAdd
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Sales = addedSales.Select(Util.SaleToView).ToList(),
                }, cancellationToken);

                await _uploadLogDb.LogAction(new UploadLogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Event = "SalesUploadSuccess",
                    Application = source.Name,
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = parameters.Listings?.Count ?? -1,
                    Sales = addedSales.Count,
                });
            }
        }

        if (parameters.Listings != null)
        {
            if (parameters.Listings.Any(l =>
                    Util.HasHtmlTags(l.ListingId) || Util.HasHtmlTags(l.RetainerName) ||
                    Util.HasHtmlTags(l.RetainerId) || Util.HasHtmlTags(l.CreatorName) || Util.HasHtmlTags(l.SellerId) ||
                    Util.HasHtmlTags(l.CreatorId)))
            {
                await _uploadLogDb.LogAction(new UploadLogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Event = "ListingsUploadMalformed",
                    Application = source.Name,
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = parameters.Listings?.Count ?? -1,
                    Sales = parameters.Sales?.Count ?? -1,
                });

                return new BadRequestResult();
            }

            var newListings = CleanUploadedListings(parameters.Listings, itemId, worldId, source.Name);

            var oldListings = existingCurrentlyShown?.Listings ?? new List<Listing>();
            var addedListings = newListings.Where(l => !oldListings.Contains(l)).ToList();
            var removedListings = oldListings.Where(l => !newListings.Contains(l)).ToList();

            if (addedListings.Count > 0)
            {
                _ = _bus?.Publish(new ListingsAdd
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = addedListings
                        .Select(Util.ListingToView)
                        .ToList(),
                }, cancellationToken);
            }

            if (removedListings.Count > 0)
            {
                _ = _bus?.Publish(new ListingsRemove
                {
                    WorldId = worldId,
                    ItemId = itemId,
                    Listings = removedListings
                        .Select(Util.ListingToView)
                        .ToList(),
                }, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var document = new CurrentlyShown
            {
                WorldId = worldId,
                ItemId = itemId,
                LastUploadTimeUnixMilliseconds = now,
                UploadSource = source.Name,
                Listings = newListings,
            };
            await _currentlyShownDb.Update(document, new CurrentlyShownQuery
            {
                WorldId = worldId,
                ItemId = itemId,
            }, cancellationToken);

            await _uploadLogDb.LogAction(new UploadLogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Event = "ListingsUploadSuccess",
                Application = source.Name,
                WorldId = worldId,
                ItemId = itemId,
                Listings = newListings.Count,
                Sales = parameters.Sales?.Count ?? -1,
            });
        }

        return null;
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
                    LastReviewTime = DateTimeOffset.FromUnixTimeSeconds(l.LastReviewTimeUnixSeconds ?? 0).UtcDateTime,
                    RetainerId = Util.ParseUnusualId(l.RetainerId) ?? "",
                    RetainerName = l.RetainerName,
                    RetainerCityId = l.RetainerCityId ?? 0,
                    SellerId = Util.ParseUnusualId(l.SellerId) ?? "",
                    Source = sourceName,
                };
            })
            .Where(l => l.PricePerUnit > 0)
            .Where(l => l.Quantity > 0)
            .OrderBy(l => l.PricePerUnit)
            .ToList();
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