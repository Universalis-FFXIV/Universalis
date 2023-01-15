using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Attributes;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.GameData;

namespace Universalis.Application.Uploads.Behaviors;

[Validator]
public class ItemIdUploadBehavior : IUploadBehavior
{
    private readonly IGameDataProvider _gameData;
    private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;

    public ItemIdUploadBehavior(IGameDataProvider gameData, IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
    {
        _gameData = gameData;
        _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return parameters.ItemId != null;
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("ItemIdUploadBehavior.Execute");

        if (!_gameData.MarketableItemIds().Contains(parameters.ItemId!.Value))
        {
            return new NotFoundObjectResult(parameters.ItemId);
        }

        await _recentlyUpdatedItemsDb.Push(parameters.ItemId.Value, cancellationToken);
        return null;
    }
}