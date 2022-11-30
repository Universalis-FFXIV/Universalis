using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
public class UploadCountHistoryController : ControllerBase
{
    private readonly IUploadCountHistoryDbAccess _uploadCountHistoryDb;
    
    public UploadCountHistoryController(IUploadCountHistoryDbAccess uploadCountHistoryDb)
    {
        _uploadCountHistoryDb = uploadCountHistoryDb;
    }

    /// <summary>
    /// Returns the number of uploads per day over the past 30 days.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("1")]
    [ApiTag("Uploads per day")]
    [Route("extra/stats/upload-history")]
    [ProducesResponseType(typeof(UploadCountHistoryView), 200)]
    public async Task<UploadCountHistoryView> Get(CancellationToken cancellationToken = default)
    {
        var data = await _uploadCountHistoryDb.GetUploadCounts(29, cancellationToken);
        return new UploadCountHistoryView
        {
            UploadCountByDay = data.Select(Convert.ToDouble).ToList(),
        };
    }
    
    /// <summary>
    /// Returns the number of uploads per day over the past 30 days.
    /// </summary>
    [HttpGet]
    [MapToApiVersion("2")]
    [ApiTag("Uploads per day")]
    [Route("v{version:apiVersion}/extra/stats/upload-history")]
    [ProducesResponseType(typeof(UploadCountHistoryView), 200)]
    public Task<UploadCountHistoryView> GetV2(CancellationToken cancellationToken = default)
    {
        return Get(cancellationToken);
    }
}