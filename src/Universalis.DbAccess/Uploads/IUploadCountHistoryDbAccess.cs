using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IUploadCountHistoryDbAccess
{
    Task Increment();

    Task<IList<long>> GetUploadCounts(int stop = -1);
}