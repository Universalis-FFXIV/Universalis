using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public class UploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
{
    private readonly IDailyUploadCountStore _store;
    
    public UploadCountHistoryDbAccess(IDailyUploadCountStore store)
    {
        _store = store;
    }

    public Task Increment()
    {
        return _store.Increment();
    }

    public Task<IList<long>> GetUploadCounts(int stop = -1)
    {
        return _store.GetUploadCounts(stop);
    }
}