using System.Collections.Generic;
using System.Linq;
using Universalis.Common.GameData;
using Universalis.GameData;

namespace Universalis.Application.Common;

public class WorldToDcRegion : IWorldToDcRegion
{
    private readonly Dictionary<int, DataCenter> _worldToDc = new();

    public WorldToDcRegion(IGameDataProvider gameDataProvider)
    {
        foreach (var dataCenter in gameDataProvider.DataCenters())
        {
            foreach (var worldId in dataCenter.WorldIds)
            {
                _worldToDc[worldId] = dataCenter;
            }
        }
    }

    public (string Dc, string Region) Get(int worldId)
    {
        var dc = _worldToDc[worldId];
        return (dc.Name, dc.Region);
    }
}
