using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Views;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats
{
    [ApiController]
    [Route("api/extra/stats/upload-history")]
    public class UploadCountHistoryController : ControllerBase
    {
        private readonly IUploadCountHistoryDbAccess _uploadCountHistoryDb;

        public UploadCountHistoryController(IUploadCountHistoryDbAccess uploadCountHistoryDb)
        {
            _uploadCountHistoryDb = uploadCountHistoryDb;
        }

        [HttpGet]
        public async Task<UploadCountHistoryView> Get()
        {
            var data = await _uploadCountHistoryDb.Retrieve(new UploadCountHistoryQuery());
            return new UploadCountHistoryView
            {
                UploadCountByDay = data?.UploadCountByDay ?? new List<uint>(),
            };
        }
    }
}