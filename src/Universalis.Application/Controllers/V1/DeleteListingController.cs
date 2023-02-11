using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Realtime;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Uploads.Schema;
using Universalis.Application.Views.V1;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api")]
public class DeleteListingController : WorldDcRegionControllerBase
{
    private readonly ITrustedSourceDbAccess _trustedSourceDb;
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
    private readonly IUploadLogDbAccess _uploadLogDb;
    private readonly ISocketProcessor _sockets;

    public DeleteListingController(
        IGameDataProvider gameData,
        ITrustedSourceDbAccess trustedSourceDb,
        ICurrentlyShownDbAccess currentlyShownDb,
        IFlaggedUploaderDbAccess flaggedUploaderDb,
        IUploadLogDbAccess uploadLogDb,
        ISocketProcessor sockets) : base(gameData)
    {
        _trustedSourceDb = trustedSourceDb;
        _currentlyShownDb = currentlyShownDb;
        _flaggedUploaderDb = flaggedUploaderDb;
        _uploadLogDb = uploadLogDb;
        _sockets = sockets;
    }

    [HttpPost]
    [MapToApiVersion("1")]
    [Route("{world}/{itemId}/delete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Post(int itemId, string world, [FromHeader] string authorization,
        [FromBody] DeleteListingParameters parameters, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("DeleteListingControllerV1.Post");
        activity?.AddTag("itemId", itemId);
        activity?.AddTag("worldDcRegion", world);

        var source = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
        {
            ApiKeySha512 = await TrustedSourceHashCache.GetHash(authorization, _trustedSourceDb, cancellationToken),
        }, cancellationToken);

        if (source == null)
        {
            return Forbid();
        }

        activity?.AddTag("source", source.Name);

        if (!TryGetWorldDc(world, out var worldDc) || !worldDc.IsWorld || string.IsNullOrEmpty(parameters.UploaderId))
        {
            return BadRequest();
        }

        // Hash the uploader ID
        using (var sha256 = SHA256.Create())
        {
            parameters.UploaderId = Util.Hash(sha256, parameters.UploaderId);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        // Check if this uploader is flagged, cancel if they are
        if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderIdSha256 = parameters.UploaderId },
                cts.Token) != null)
        {
            return Ok("Success");
        }

        // Remove the listing
        var itemData = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldDc.WorldId,
            ItemId = itemId,
        }, cts.Token);
        if (itemData == null)
        {
            // No item data; nothing to remove
            return Ok("Success");
        }

        var listingIndex = itemData.Listings.FindIndex(listing =>
            listing.RetainerId == parameters.RetainerId
            && listing.Quantity == parameters.Quantity
            && listing.PricePerUnit == parameters.PricePerUnit);
        if (listingIndex == -1)
        {
            // No matching listing; nothing to remove
            return Ok("Success");
        }

        var listing = itemData.Listings[listingIndex];

        // TODO: Allow for deleting a single listing directly
        itemData.Listings.RemoveAt(listingIndex);

        var query = new CurrentlyShownQuery
        {
            WorldId = worldDc.WorldId,
            ItemId = itemId,
        };

        await _currentlyShownDb.Update(itemData, query, cts.Token);

        // Log the upload
        await _uploadLogDb.LogAction(new UploadLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            Event = "DeleteListing",
            Application = source.Name,
            WorldId = worldDc.WorldId,
            ItemId = itemId,
            Listings = 1,
            Sales = 0,
        });

        _sockets.Publish(new ListingsRemove
        {
            WorldId = query.WorldId,
            ItemId = query.ItemId,
            Listings = new List<ListingView> { Util.ListingToView(listing) },
        });

        return Ok("Success");
    }

    [HttpPost]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/{world}/{itemId}/delete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> PostV2(int itemId, string world, [FromHeader] string authorization,
        [FromBody] DeleteListingParameters parameters, CancellationToken cancellationToken = default)
    {
        return Post(itemId, world, authorization, parameters, cancellationToken);
    }
}