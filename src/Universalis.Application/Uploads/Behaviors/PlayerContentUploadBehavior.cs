using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess;
using Universalis.DbAccess.Queries;
using Universalis.Entities;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors;

public class PlayerContentUploadBehavior : IUploadBehavior
{
    private readonly IContentDbAccess _contentDb;

    public PlayerContentUploadBehavior(IContentDbAccess contentDb)
    {
        _contentDb = contentDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return !string.IsNullOrEmpty(parameters.ContentId) && !string.IsNullOrEmpty(parameters.CharacterName);
    }

    public async Task<IActionResult?> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        await _contentDb.Update(new Content
        {
            ContentId = parameters.ContentId,
            ContentType = ContentKind.Player,
            CharacterName = parameters.CharacterName,
        }, new ContentQuery
        {
            ContentId = parameters.ContentId,
        }, cancellationToken);

        return null;
    }
}