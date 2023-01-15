using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.AccessControl;

namespace Universalis.Application.Uploads.Behaviors;

public class DailyUploadIncrementUploadBehavior : IUploadBehavior
{
    private readonly IUploadCountHistoryDbAccess _uploadCountHistoryDb;

    public DailyUploadIncrementUploadBehavior(IUploadCountHistoryDbAccess uploadCountHistoryDb)
    {
        _uploadCountHistoryDb = uploadCountHistoryDb;
    }

    public bool ShouldExecute(UploadParameters parameters)
    {
        return true;
    }

    public async Task<IActionResult> Execute(ApiKey source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("DailyUploadIncrementUploadBehavior.Execute");

        await _uploadCountHistoryDb.Increment();
        return null;
    }
}