using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IWorldItemUploadStore
{
    Task SetItem(uint worldId, uint id, double val);

    Task<IList<KeyValuePair<uint, double>>> GetMostRecent(uint worldId, int stop = -1);
    
    Task<IList<KeyValuePair<uint, double>>> GetLeastRecent(uint worldId, int stop = -1);
}