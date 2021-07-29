using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public class SourceIncrementUploadBehavior : IUploadBehavior
    {
        private readonly ITrustedSourceDbAccess _trustedSourceDb;

        public SourceIncrementUploadBehavior(ITrustedSourceDbAccess trustedSourceDb)
        {
            _trustedSourceDb = trustedSourceDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return true;
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters)
        {
            await _trustedSourceDb.Increment(new TrustedSourceQuery
            {
                ApiKeySha256 = source.ApiKeySha256,
            });

            return null;
        }
    }
}