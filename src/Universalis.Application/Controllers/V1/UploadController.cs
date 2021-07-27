using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploaders;
using Universalis.GameData;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/upload/{apiKey}")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IHistoryDbAccess _historyDb;
        private readonly ITaxRatesDbAccess _taxRatesDb;
        private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
        private readonly IWorldUploadCountDbAccess _worldUploadCountDb;
        private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;

        public UploadController(
            IGameDataProvider gameData,
            ITrustedSourceDbAccess trustedSourceDb,
            ICurrentlyShownDbAccess currentlyShownDb,
            IHistoryDbAccess historyDb,
            ITaxRatesDbAccess taxRatesDb,
            IFlaggedUploaderDbAccess flaggedUploaderDb,
            IWorldUploadCountDbAccess worldUploadCountDb,
            IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
        {
            _gameData = gameData;
            _trustedSourceDb = trustedSourceDb;
            _currentlyShownDb = currentlyShownDb;
            _historyDb = historyDb;
            _taxRatesDb = taxRatesDb;
            _flaggedUploaderDb = flaggedUploaderDb;
            _worldUploadCountDb = worldUploadCountDb;
            _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
        }

        [HttpPost]
        public async Task<IActionResult> Post(string apiKey, [FromBody] UploadParameters parameters)
        {
            TrustedSource source;
            using (var sha256 = SHA256.Create())
            {
                await using var authStream = new MemoryStream(Encoding.UTF8.GetBytes(apiKey));
                source = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
                {
                    ApiKeySha256 = BitConverter.ToString(await sha256.ComputeHashAsync(authStream)),
                });
            }

            if (source == null)
            {
                return Forbid();
            }

            // Hash the uploader ID
            using (var sha256 = SHA256.Create())
            {
                await using var uploaderIdStream = new MemoryStream(Encoding.UTF8.GetBytes(parameters.UploaderId));
                parameters.UploaderId = BitConverter.ToString(await sha256.ComputeHashAsync(uploaderIdStream));
            }

            // Check if this uploader is flagged, cancel if they are
            if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderId = parameters.UploaderId }) !=
                null)
            {
                return Ok("Success");
            }

            // Store data
            if (parameters.WorldId != null)
            {
                var worldName = _gameData.AvailableWorlds()[parameters.WorldId.Value];
                await _worldUploadCountDb.Increment(new WorldUploadCountQuery { WorldName = worldName });
            }

            if (parameters.ItemId != null)
            {
                await _recentlyUpdatedItemsDb.Push(parameters.ItemId.Value);
            }

            if (parameters.WorldId != null && parameters.TaxRates != null)
            {
                await _taxRatesDb.Update(new TaxRates
                {
                    SetName = TaxRatesQuery.SetName,
                    LimsaLominsa = parameters.TaxRates.LimsaLominsa,
                    Gridania = parameters.TaxRates.Gridania,
                    Uldah = parameters.TaxRates.Uldah,
                    Ishgard = parameters.TaxRates.Ishgard,
                    Kugane = parameters.TaxRates.Kugane,
                    Crystarium = parameters.TaxRates.Crystarium,
                    UploaderIdHash = parameters.UploaderId,
                    WorldId = parameters.WorldId.Value,
                    UploadApplicationName = source.Name,
                }, new TaxRatesQuery
                {
                    WorldId = parameters.WorldId.Value,
                });
            }

            return Ok(); // TODO
        }

        public class UploadParameters
        {
            [JsonProperty("uploaderID")]
            public string UploaderId { get; set; }

            [JsonProperty("worldID")]
            public uint? WorldId { get; set; }

            [JsonProperty("itemID")]
            public uint? ItemId { get; set; }

            [JsonProperty("marketTaxRates")]
            public MarketTaxRates TaxRates { get; set; }
        }

        public class MarketTaxRates
        {
            [JsonProperty("limsaLominsa")]
            public byte LimsaLominsa { get; set; }

            [JsonProperty("gridania")]
            public byte Gridania { get; set; }

            [JsonProperty("uldah")]
            public byte Uldah { get; set; }

            [JsonProperty("ishgard")]
            public byte Ishgard { get; set; }

            [JsonProperty("kugane")]
            public byte Kugane { get; set; }

            [JsonProperty("crystarium")]
            public byte Crystarium { get; set; }
        }
    }
}
