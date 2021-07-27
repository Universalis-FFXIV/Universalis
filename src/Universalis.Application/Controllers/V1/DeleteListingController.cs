using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities.Uploaders;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/{itemId}/{world}/delete")]
    [ApiController]
    public class DeleteListingController : WorldDcControllerBase
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;

        public DeleteListingController(
            IGameDataProvider gameData,
            ITrustedSourceDbAccess trustedSourceDb,
            ICurrentlyShownDbAccess currentlyShownDb) : base(gameData)
        {
            _trustedSourceDb = trustedSourceDb;
            _currentlyShownDb = currentlyShownDb;
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
                await using var uploaderIdStream = new MemoryStream(Encoding.UTF8.GetBytes(authorization));
                parameters.UploaderId = BitConverter.ToString(await sha256.ComputeHashAsync(uploaderIdStream));
            }

            // TODO: blacklist check

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

        public class DeleteListingParameters
        {
            [JsonProperty("retainerID")]
            public string RetainerId { get; set; }

            [JsonProperty("listingID")]
            public string ListingId { get; set; }

            [JsonProperty("quantity")]
            public uint Quantity { get; set; }

            [JsonProperty("pricePerUnit")]
            public uint PricePerUnit { get; set; }

            [JsonProperty("uploaderID")]
            public string UploaderId { get; set; }
        }
    }
}
