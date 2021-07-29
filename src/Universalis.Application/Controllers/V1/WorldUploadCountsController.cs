using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1
{
    [ApiController]
    [Route("api/extra/stats/world-upload-counts")]
    public class WorldUploadCountsController : ControllerBase
    {
        private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

        public WorldUploadCountsController(IWorldUploadCountDbAccess worldUploadCountDb)
        {
            _worldUploadCountDb = worldUploadCountDb;
        }

        [HttpGet]
        public async Task<IEnumerable<WorldUploadCountView>> Get()
        {
            var data = (await _worldUploadCountDb.GetWorldUploadCounts()).ToList();
            var sum = data.Sum(d => d.Count);
            return data
                .Select(d => new WorldUploadCountView
                {
                    Count = d.Count,
                    Proportion = (double)d.Count / sum,
                });
        }
    }
}