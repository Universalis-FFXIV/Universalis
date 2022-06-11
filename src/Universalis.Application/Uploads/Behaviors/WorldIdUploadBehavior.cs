using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Attributes;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;
using Universalis.GameData;

namespace Universalis.Application.Uploads.Behaviors;

[Validator]
public class WorldIdUploadBehavior : IUploadBehavior
{
    private readonly IGameDataProvider _gameData;
    private readonly IWorldUploadCountDbAccess _worldUploadCountDb;

    public WorldIdUploadBehavior(IGameDataProvider gameData, IWorldUploadCountDbAccess worldUploadCountDb)
    {
        _gameData = gameData;
        _worldUploadCountDb = worldUploadCountDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return parameters.WorldId != null;
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        var worldId = parameters.WorldId!.Value;

        if (!_gameData.AvailableWorldIds().Contains(worldId))
            return new NotFoundObjectResult(worldId);

        var worldName = _gameData.AvailableWorlds()[parameters.WorldId.Value];
        await _worldUploadCountDb.Increment(new WorldUploadCountQuery { WorldName = worldName }, cancellationToken);

        return null;
    }
}