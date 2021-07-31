using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/{itemId}/{world}/delete")]
    public class DeleteListingController : WorldDcControllerBase
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;

        public DeleteListingController(
            IGameDataProvider gameData,
            ITrustedSourceDbAccess trustedSourceDb,
            ICurrentlyShownDbAccess currentlyShownDb,
            IFlaggedUploaderDbAccess flaggedUploaderDb) : base(gameData)
        {
            _trustedSourceDb = trustedSourceDb;
            _currentlyShownDb = currentlyShownDb;
            _flaggedUploaderDb = flaggedUploaderDb;
        }

        [HttpPost]
        public async Task<IActionResult> Post(uint itemId, string world, [FromHeader] string authorization, [FromBody] DeleteListingParameters parameters)
        {
            TrustedSource source;
            using (var sha256 = SHA256.Create())
            {
                await using var authStream = new MemoryStream(Encoding.UTF8.GetBytes(authorization));
                source = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
                {
                    ApiKeySha256 = BitConverter.ToString(await sha256.ComputeHashAsync(authStream)),
                });
            }

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
                parameters.UploaderId = BitConverter.ToString(await sha256.ComputeHashAsync(uploaderIdStream));
            }

            // Check if this uploader is flagged, cancel if they are
            if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = parameters.UploaderId }) != null)
            {
                return Ok("Success");
            }

            // Remove the listing
            var itemData = await _currentlyShownDb.Retrieve(new CurrentlyShownQuery
            {
                WorldId = worldDc.WorldId,
                ItemId = itemId,
            });

            if (itemData == null)
            {
                return Ok("Success");
            }

            var listingIndex = itemData.Listings.FindIndex(listing =>
                listing.RetainerId == parameters.RetainerId
                && listing.Quantity == parameters.Quantity
                && listing.PricePerUnit == parameters.PricePerUnit);

            itemData.Listings.RemoveAt(listingIndex);

            await _currentlyShownDb.Update(itemData, new CurrentlyShownQuery
            {
                WorldId = worldDc.WorldId,
                ItemId = itemId,
            });

            return Ok("Success");
        }
    }
}
