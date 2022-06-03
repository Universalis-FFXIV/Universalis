using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldItemUploadStore
{
    Task SetItem(string key, uint id, double val);

    Task<IList<KeyValuePair<uint, double>>> GetAllItems(string key, int stop = -1);
}