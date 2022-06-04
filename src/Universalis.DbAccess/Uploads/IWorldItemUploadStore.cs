using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldItemUploadStore
{
    Task SetItem(string key, uint id, double val);

    Task<IList<KeyValuePair<uint, double>>> GetMostRecent(string key, int stop = -1);
    
    Task<IList<KeyValuePair<uint, double>>> GetLeastRecent(string key, int stop = -1);
}