using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.DbAccess.Queries.Uploads;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

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

    public async Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default)
    {
        await _uploadCountHistoryDb.Increment();
        return null;
    }
}