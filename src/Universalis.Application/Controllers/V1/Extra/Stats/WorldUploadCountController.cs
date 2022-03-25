using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/world-upload-counts")]
public class WorldUploadCountController : ControllerBase
{
    private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

    public WorldUploadCountController(IWorldUploadCountDbAccess worldUploadCountDb)
    {
        _worldUploadCountDb = worldUploadCountDb;
    }

    /// <summary>
    /// Returns the world upload counts and proportions of the total uploads for each world.
    /// </summary>
    [HttpGet]
    [ApiTag("Upload counts by world")]
    [ProducesResponseType(typeof(IDictionary<string, WorldUploadCountView>), 200)]
    public async Task<IDictionary<string, WorldUploadCountView>> Get(CancellationToken cancellationToken = default)
    {
        var data = (await _worldUploadCountDb.GetWorldUploadCounts(cancellationToken))
            .Where(d => !string.IsNullOrEmpty(d.WorldName))
            .ToList();

        var sum = data.Sum(d => d.Count);
        return data
            .ToDictionary(d => d.WorldName, d => new WorldUploadCountView
            {
                Count = d.Count,
                Proportion = d.Count / sum,
            });
    }
}