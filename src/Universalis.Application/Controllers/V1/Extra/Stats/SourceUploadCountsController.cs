using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
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
    [ApiTag("Upload counts by upload application")]
    [ProducesResponseType(typeof(IEnumerable<SourceUploadCountView>), 200)]
    public async Task<IEnumerable<SourceUploadCountView>> Get(CancellationToken cancellationToken = default)
    {
        var data = await _trustedSourceDb.GetUploaderCounts(cancellationToken);
        return data
            .Select(d => new SourceUploadCountView
            {
                Name = d.Name,
                UploadCount = d.UploadCount,
            })
            .ToList();
    }
}