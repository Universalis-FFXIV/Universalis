using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess;
using Universalis.Entities;
using Universalis.Entities.AccessControl;

namespace Universalis.Application.Uploads.Behaviors;

public class PlayerContentUploadBehavior : IUploadBehavior
{
    private readonly ICharacterDbAccess _characterDb;

    public PlayerContentUploadBehavior(ICharacterDbAccess characterDb)
    {
        _characterDb = characterDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return !string.IsNullOrEmpty(parameters.ContentId) && ulong.TryParse(parameters.ContentId, out _) && !string.IsNullOrEmpty(parameters.CharacterName);
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var contentIdHash = Util.Hash(sha256, parameters.ContentId);
        
        var existing = await _characterDb.Retrieve(contentIdHash, cancellationToken);
        var character = new Character(contentIdHash, parameters.CharacterName, existing?.WorldId);
        
        if (existing == null)
        {
            await _characterDb.Create(character, cancellationToken);
        }
        else
        {
            await _characterDb.Update(character, cancellationToken);
        }

        return null;
    }
}