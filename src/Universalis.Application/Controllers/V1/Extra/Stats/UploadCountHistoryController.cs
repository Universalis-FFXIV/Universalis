using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/upload-history")]
public class UploadCountHistoryController : ControllerBase
{
    private readonly IUploadCountHistoryDbAccess _uploadCountHistoryDb;
    
    // Bodge caching mechanism; TODO: fix
    private static UploadCountHistory Data;
    private static DateTime LastFetch = DateTime.Now;

    public UploadCountHistoryController(IUploadCountHistoryDbAccess uploadCountHistoryDb)
    {
        _uploadCountHistoryDb = uploadCountHistoryDb;
    }

    /// <summary>
    /// Returns the number of uploads per day over the past 30 days.
    /// </summary>
    [HttpGet]
    [ApiTag("Uploads per day")]
    [ProducesResponseType(typeof(UploadCountHistoryView), 200)]
    public async Task<UploadCountHistoryView> Get(CancellationToken cancellationToken = default)
    {
        if (DateTime.Now - LastFetch < TimeSpan.FromMinutes(5))
        {
            Data = await _uploadCountHistoryDb.Retrieve(new UploadCountHistoryQuery(), cancellationToken);
            LastFetch = DateTime.Now;
        }
        
        return new UploadCountHistoryView
        {
            UploadCountByDay = Data?.UploadCountByDay ?? new List<double>(),
        };
    }
}