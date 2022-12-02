using System.Collections.Generic;
using System.Threading.Tasks;

namespace Universalis.DbAccess.Uploads;

public interface IRecentlyUpdatedItemsStore
{
    Task SetItem(int id, double val);
    
    Task<IList<KeyValuePair<int, double>>> GetAllItems(int stop = -1);
}