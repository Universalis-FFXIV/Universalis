using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.Entities.Uploads;
using Universalis.GameData;

namespace Universalis.Application.Uploads.Behaviors;

public class MostRecentlyUpdatedUploadBehavior : IUploadBehavior
{
    private readonly IGameDataProvider _gameData;
    private readonly IMostRecentlyUpdatedDbAccess _mostRecentlyUpdatedDb;

    public MostRecentlyUpdatedUploadBehavior(IGameDataProvider gameData, IMostRecentlyUpdatedDbAccess mostRecentlyUpdatedDb)
    {
        _gameData = gameData;
        _mostRecentlyUpdatedDb = mostRecentlyUpdatedDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return parameters.ItemId.HasValue
               && parameters.WorldId.HasValue
               && _gameData.AvailableWorldIds().Contains(parameters.WorldId.Value);
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        var worldId = parameters.WorldId!.Value;

        await _mostRecentlyUpdatedDb.Push(worldId, new WorldItemUpload
        {
            ItemId = parameters.ItemId!.Value,
            WorldId = worldId,
            LastUploadTimeUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        }, cancellationToken);

        return null;
    }
}