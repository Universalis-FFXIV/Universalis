using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IRecentlyUpdatedItemsStore
{
    Task SetItem(uint id, double val);
    
    Task<IList<KeyValuePair<uint, double>>> GetAllItems(int stop = -1);
}