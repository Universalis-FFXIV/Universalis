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
    [Route("api/upload/{apiKey}")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IGameDataProvider _gameData;
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly ICurrentlyShownDbAccess _currentlyShownDb;
        private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
        private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

        public UploadController(
            IGameDataProvider gameData,
            ITrustedSourceDbAccess trustedSourceDb,
            ICurrentlyShownDbAccess currentlyShownDb,
            IFlaggedUploaderDbAccess flaggedUploaderDb,
            IWorldUploadCountDbAccess worldUploadCountDb)
        {
            _gameData = gameData;
            _trustedSourceDb = trustedSourceDb;
            _currentlyShownDb = currentlyShownDb;
            _flaggedUploaderDb = flaggedUploaderDb;
            _worldUploadCountDb = worldUploadCountDb;
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

            if (parameters.WorldId != null)
            {
                var worldName = _gameData.AvailableWorlds()[parameters.WorldId.Value];
                await _worldUploadCountDb.Increment(new WorldUploadCountQuery { WorldName = worldName });
            }

            return Ok(); // TODO
        }

        public class UploadParameters
        {
            [JsonProperty("uploaderID")]
            public string UploaderId { get; set; }

            [JsonProperty("worldID")]
            public uint? WorldId { get; set; }
        }
    }
}
