using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public class MostRecentlyUpdatedUploadBehavior : IUploadBehavior
    {
        private readonly IMostRecentlyUpdatedDbAccess _mostRecentlyUpdatedDb;

        public MostRecentlyUpdatedUploadBehavior(IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb)
        {
            _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return parameters.ItemId.HasValue && parameters.WorldId.HasValue;
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default)
        {
            await _mostRecentlyUpdatedDb.Create(new WorldItemUpload
            {
                // ReSharper disable PossibleInvalidOperationException
                ItemId = parameters.ItemId.Value,
                WorldId = parameters.WorldId.Value,
                // ReSharper enable PossibleInvalidOperationException
                LastUploadTimeUnixMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            }, cancellationToken);

            return null;
        }
    }
}