using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.AccessControl;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
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
    [MapToApiVersion("1")]
    [ApiTag("Upload counts by upload application")]
    [Route("extra/stats/uploader-upload-counts")]
    [ProducesResponseType(typeof(IEnumerable<SourceUploadCountView>), 200)]
    public async Task<IEnumerable<SourceUploadCountView>> Get(CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var data = await _trustedSourceDb.GetUploaderCounts(cts.Token);
        return data
            .Select(d => new SourceUploadCountView
            {
                Name = d.Name,
                UploadCount = d.UploadCount,
            })
            .ToList();
    }

    /// <summary>
    /// Returns the total upload counts for each client application that uploads data to Universalis.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Upload counts by upload application")]
    [Route("v{version:apiVersion}/extra/stats/uploader-upload-counts")]
    [ProducesResponseType(typeof(IEnumerable<SourceUploadCountView>), 200)]
    public Task<IEnumerable<SourceUploadCountView>> GetV2(CancellationToken cancellationToken = default)
    {
        return Get(cancellationToken);
    }
}