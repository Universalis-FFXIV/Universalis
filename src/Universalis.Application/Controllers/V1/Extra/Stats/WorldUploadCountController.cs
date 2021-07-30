using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats
{
    [ApiController]
    [Route("api/extra/stats/world-upload-counts")]
    public class WorldUploadCountController : ControllerBase
    {
        private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

        public WorldUploadCountController(IWorldUploadCountDbAccess worldUploadCountDb)
        {
            _worldUploadCountDb = worldUploadCountDb;
        }

        [HttpGet]
        public async Task<IDictionary<string, WorldUploadCountView>> Get()
        {
            var data = (await _worldUploadCountDb.GetWorldUploadCounts())?.ToList();
            if (data == null)
            {
                return new Dictionary<string, WorldUploadCountView>();
            }

            var sum = data.Sum(d => d.Count);
            return data
                .ToDictionary(d => d.WorldName, d => new WorldUploadCountView
                {
                    Count = d.Count,
                    Proportion = (double)d.Count / sum,
                });
        }
    }
}