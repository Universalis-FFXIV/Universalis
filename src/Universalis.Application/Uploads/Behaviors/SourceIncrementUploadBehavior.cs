using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.AccessControl;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.Entities.AccessControl;

namespace Universalis.Application.Uploads.Behaviors;

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

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("SourceIncrementUploadBehavior.Execute");

        var ts = await _trustedSourceDb.Retrieve(new TrustedSourceQuery
        {
            ApiKeySha512 = source.TokenSha512,
        }, cancellationToken);

        if (ts == null)
        {
            return null;
        }

        await _trustedSourceDb.Increment(new TrustedSourceQuery { ApiKeySha512 = ts.TokenSha512 }, cancellationToken);

        return null;
    }
}