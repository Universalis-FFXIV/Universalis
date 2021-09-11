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

namespace Universalis.Application.Uploads.Behaviors
{
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
            var now = (double)DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var data = await _uploadCountHistoryDb.Retrieve(new UploadCountHistoryQuery(), cancellationToken);
            if (data == null)
            {
                await _uploadCountHistoryDb.Create(new UploadCountHistory
                {
                    LastPush = now,
                    UploadCountByDay = new List<double> { 1 },
                }, cancellationToken);

                return null;
            }

            if (now - data.LastPush > 86400000)
            {
                data.LastPush = now;
                data.UploadCountByDay = new double[] { 0 }.Concat(data.UploadCountByDay ?? new List<double>()).Take(30).ToList();
            }

            data.UploadCountByDay[0]++;
            await _uploadCountHistoryDb.Update(data.LastPush, data.UploadCountByDay, cancellationToken);

            return null;
        }
    }
}