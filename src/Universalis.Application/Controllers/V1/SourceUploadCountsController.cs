using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1
{
    [Route("api/extra/stats/uploader-upload-counts")]
    [ApiController]
    public class SourceUploadCountsController : ControllerBase
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;

        public SourceUploadCountsController(ITrustedSourceDbAccess trustedSourceDb)
        {
            _trustedSourceDb = trustedSourceDb;
        }

        [HttpGet]
        public async Task<IEnumerable<SourceUploadCountsView>> Get()
        {
            var data = await _trustedSourceDb.GetUploaderCounts();
            return data
                .Select(d => new SourceUploadCountsView
                {
                    Name = d.Name,
                    UploadCount = d.UploadCount,
                });
        }
    }
}