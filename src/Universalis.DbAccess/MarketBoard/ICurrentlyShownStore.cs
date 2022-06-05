using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Entities.MarketBoard;

namespace Universalis.DbAccess.MarketBoard;

public interface ICurrentlyShownStore
{
    Task<CurrentlyShownSimple> GetData(uint worldId, uint itemId);

    Task SetData(CurrentlyShownSimple data);
}