using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldItemUploadStore
{
    Task SetItem(int worldId, int itemId, double val);

    Task<IList<KeyValuePair<int, double>>> GetMostRecent(int worldId, int stop = -1);

    Task<IList<KeyValuePair<int, double>>> GetLeastRecent(int worldId, int stop = -1);

    Task<double?> GetUploadTime(int worldId, int itemId);
}