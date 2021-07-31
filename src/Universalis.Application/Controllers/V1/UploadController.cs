using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/upload/{apiKey}")]
    public class UploadController : ControllerBase
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;
        private readonly IFlaggedUploaderDbAccess _flaggedUploaderDb;
        private readonly IEnumerable<IUploadBehavior> _uploadBehaviors;

        public UploadController(
            ITrustedSourceDbAccess trustedSourceDb,
            IFlaggedUploaderDbAccess flaggedUploaderDb,
            IEnumerable<IUploadBehavior> uploadBehaviors)
        {
            _trustedSourceDb = trustedSourceDb;
            _flaggedUploaderDb = flaggedUploaderDb;
            _uploadBehaviors = uploadBehaviors;
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

            if (string.IsNullOrEmpty(parameters.UploaderId))
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
            if (await _flaggedUploaderDb.Retrieve(new FlaggedUploaderQuery { UploaderIdHash = parameters.UploaderId }) !=
                null)
            {
                return Ok("Success");
            }

            // Execute upload behaviors
            foreach (var uploadBehavior in _uploadBehaviors)
            {
                if (!uploadBehavior.ShouldExecute(parameters)) continue;
                var actionResult = await uploadBehavior.Execute(source, parameters);
                if (uploadBehavior.GetType().GetCustomAttribute<ValidatorAttribute>() != null
                    && actionResult != null)
                {
                    return actionResult;
                }
            }

            return Ok("Success");
        }
    }
}
