using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IDailyUploadCountStore
{
    Task Increment(CancellationToken cancellationToken = default);

    Task<IList<long>> GetUploadCounts(int stop = -1, CancellationToken cancellationToken = default);
}