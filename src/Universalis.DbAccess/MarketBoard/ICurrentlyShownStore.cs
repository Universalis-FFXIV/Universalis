using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownStore
{
    Task<CurrentlyShown> GetData(uint worldId, uint itemId);

    Task SetData(CurrentlyShown data);
}