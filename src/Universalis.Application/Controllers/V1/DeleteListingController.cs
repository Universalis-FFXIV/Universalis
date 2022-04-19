using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Caching;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1;

[ApiController]
[ApiVersion("1")]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/{world}/{itemId}/delete")]
public class DeleteListingController : WorldDcControllerBase
{
    private readonly ITrustedSourceDbAccess _trustedSourceDb;
    private readonly ICurrentlyShownDbAccess _currentlyShownDb;
    private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
    private readonly ICache<CurrentlyShownQuery, MinimizedCurrentlyShownData> _cache;

    public DeleteListingController(
        IGameDataProvider gameData,
        ITrustedSourceDbAccess trustedSourceDb,
        ICurrentlyShownDbAccess currentlyShownDb,
        IFlaggedUploaderDbAccess flaggedUploaderDb,
        ICache<CurrentlyShownQuery, MinimizedCurrentlyShownData> cache) : base(gameData)
    {
        _trustedSourceDb = trustedSourceDb;
        _currentlyShownDb = currentlyShownDb;
        _flaggedUploaderDb = flaggedUploaderDb;
        _cache = cache;
    }

    [HttpPost]
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
        if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = parameters.UploaderId }, cancellationToken) != null)
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

        _cache.Delete(query);

        return Ok("Success");
    }
}