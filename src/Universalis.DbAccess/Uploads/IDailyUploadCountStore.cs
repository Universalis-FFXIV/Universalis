using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IDailyUploadCountStore
{
    Task Increment(string key, string lastPushKey, CancellationToken cancellationToken = default);

    Task<IList<long>> GetUploadCounts(string key, int stop = -1, CancellationToken cancellationToken = default);
}