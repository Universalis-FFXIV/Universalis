using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V1.Extra.Stats;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats;

[ApiController]
[ApiVersion("1")]
[Route("api/extra/stats/world-upload-counts")]
public class WorldUploadCountController : ControllerBase
{
    private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

    // Bodge caching mechanism; TODO: fix
    private static IList<WorldUploadCount> Data;
    private static DateTime LastFetch;

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
        if (DateTime.Now - LastFetch > TimeSpan.FromMinutes(5))
        {
            Data = (await _worldUploadCountDb.GetWorldUploadCounts(cancellationToken))
                .Where(d => !string.IsNullOrEmpty(d.WorldName))
                .ToList();
            LastFetch = DateTime.Now;
        }

        var sum = Data?.Sum(d => d.Count) ?? 0;
        return Data?
            .ToDictionary(d => d.WorldName, d => new WorldUploadCountView
            {
                Count = d.Count,
                Proportion = d.Count / sum,
            }) ?? new Dictionary<string, WorldUploadCountView>();
    }
}