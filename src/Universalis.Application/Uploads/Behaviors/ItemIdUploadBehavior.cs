using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public class ItemIdUploadBehavior : IUploadBehavior
    {
        private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;

        public ItemIdUploadBehavior(IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
        {
            _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
        }

        public bool ShouldExecute(UploadParameters parameters)
        {
            return parameters.ItemId != null;
        }

        public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters)
        {
            // ReSharper disable once PossibleInvalidOperationException
            await _recentlyUpdatedItemsDb.Push(parameters.ItemId.Value);
            return null;
        }
    }
}