using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats
{
    [ApiController]
    [Route("api/extra/stats/uploader-upload-counts")]
    public class SourceUploadCountsController : ControllerBase
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;

        public SourceUploadCountsController(ITrustedSourceDbAccess trustedSourceDb)
        {
            _trustedSourceDb = trustedSourceDb;
        }

        /// <summary>
        /// Returns the total upload counts for each client application that uploads data to Universalis.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SourceUploadCountView>), 200)]
        public async Task<IEnumerable<SourceUploadCountView>> Get()
        {
            var data = await _trustedSourceDb.GetUploaderCounts();
            return data
                .Select(d => new SourceUploadCountView
                {
                    Name = d.Name,
                    UploadCount = d.UploadCount,
                })
                .ToList();
        }
    }
}