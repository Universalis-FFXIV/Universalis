using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api")]
public class DeleteListingController : WorldDcControllerBase
{
    private readonly ITrustedSourceDbAccess _trustedSourceDb;
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
    private readonly ICache<CurrentlyShownQuery, CachedCurrentlyShownData> _cache;

    public DeleteListingController(
        IGameDataProvider gameData,
        ITrustedSourceDbAccess trustedSourceDb,
        ICurrentlyShownDbAccess currentlyShownDb,
        IFlaggedUploaderDbAccess flaggedUploaderDb,
        ICache<CurrentlyShownQuery, CachedCurrentlyShownData> cache) : base(gameData)
    {
        _trustedSourceDb = trustedSourceDb;
        _currentlyShownDb = currentlyShownDb;
        _flaggedUploaderDb = flaggedUploaderDb;
        _cache = cache;
    }

    [HttpPost]
    [MapToApiVersion("1")]
    [Route("{world}/{itemId}/delete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> Post(uint itemId, string world, [FromHeader] string authorization, [FromBody] DeleteListingParameters parameters, CancellationToken cancellationToken = default)
    {
        var source = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
        {
            ApiKeySha512 = await TrustedSourceHashCache.GetHash(authorization, _trustedSourceDb, cancellationToken),
        }, cancellationToken);

        if (source == null)
        {
            return Forbid();
        }

        if (!TryGetWorldDc(world, out var worldDc) || !worldDc.IsWorld || string.IsNullOrEmpty(parameters.UploaderId))
        {
            return BadRequest();
        }

        // Hash the uploader ID
        using (var sha256 = SHA256.Create())
        {
            await using var uploaderIdStream = new MemoryStream(Encoding.UTF8.GetBytes(parameters.UploaderId));
            parameters.UploaderId = Util.BytesToString(await sha256.ComputeHashAsync(uploaderIdStream, cancellationToken));
        }

        // Check if this uploader is flagged, cancel if they are
        if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderIdSha256 = parameters.UploaderId }, cancellationToken) != null)
        {
            return Ok("Success");
        }

        // Remove the listing
        var itemData = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
        {
            WorldId = worldDc.WorldId,
            ItemId = itemId,
        }, cancellationToken);

        if (itemData == null)
        {
            return Ok("Success");
        }

        var listingIndex = itemData.Listings.FindIndex(listing =>
            listing.RetainerId == parameters.RetainerId
            && listing.Quantity == parameters.Quantity
            && listing.PricePerUnit == parameters.PricePerUnit);

        if (listingIndex == -1)
        {
            return Ok("Success");
        }

        itemData.Listings.RemoveAt(listingIndex);

        var query = new CurrentlyShownQuery
        {
            WorldId = worldDc.WorldId,
            ItemId = itemId,
        };

        await _currentlyShownDb.Update(itemData, query, cancellationToken);

        await _cache.Delete(query, cancellationToken);

        return Ok("Success");
    }

    [HttpPost]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/{world}/{itemId}/delete")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> PostV2(uint itemId, string world, [FromHeader] string authorization,
        [FromBody] DeleteListingParameters parameters, CancellationToken cancellationToken = default)
    {
        return Post(itemId, world, authorization, parameters, cancellationToken);
    }
}