using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldUploadCountStore
{
    Task Increment(string worldName);

    Task<IList<KeyValuePair<string, long>>> GetWorldUploadCounts();
}