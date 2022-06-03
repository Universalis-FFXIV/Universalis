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

    public Task<IList<long>> GetUploadCounts(int count = -1)
    {
        return count == 0
            ? Task.FromResult((IList<long>)new List<long>())
            : _store.GetUploadCounts(Key, count - 1);
    }
}