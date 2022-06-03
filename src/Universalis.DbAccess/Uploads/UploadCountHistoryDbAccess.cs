using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public class UploadCountHistoryDbAccess : IUploadCountHistoryDbAccess
{
    public static readonly string Key = "Universalis.DailyUploads";
    public static readonly string KeyLastPush = "Universalis.DailyUploadsLastPush";

    private readonly IDailyUploadCountStore _store;
    
    public UploadCountHistoryDbAccess(IDailyUploadCountStore store)
    {
        _store = store;
    }

    public Task Increment()
    {
        return _store.Increment(Key, KeyLastPush);
    }

    public Task<IList<long>> GetUploadCounts(int stop = -1)
    {
        return _store.GetUploadCounts(Key, stop);
    }
}