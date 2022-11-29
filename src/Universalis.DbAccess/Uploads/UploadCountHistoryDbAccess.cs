using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public class UploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
{
    private readonly IDailyUploadCountStore _store;
    
    public UploadCountHistoryDbAccess(IDailyUploadCountStore store)
    {
        _store = store;
    }

    public Task Increment(CancellationToken cancellationToken = default)
    {
        return _store.Increment(cancellationToken);
    }

    public Task<IList<long>> GetUploadCounts(int stop = -1, CancellationToken cancellationToken = default)
    {
        return _store.GetUploadCounts(stop, cancellationToken);
    }
}