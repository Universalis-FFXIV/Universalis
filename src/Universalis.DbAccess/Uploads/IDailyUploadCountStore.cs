using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IDailyUploadCountStore
{
    Task Increment(string key, string lastPushKey);

    Task<IList<long>> GetUploadCounts(string key, int stop = -1);
}