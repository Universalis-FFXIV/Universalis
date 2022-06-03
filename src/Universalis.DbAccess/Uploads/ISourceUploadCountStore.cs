using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface ISourceUploadCountStore
{
    Task IncrementCounter(string key, string counterName);

    Task<IList<KeyValuePair<string, long>>> GetCounterValues(string key);
}