using System.Linq;
using Universalis.Common.GameData;
using Universalis.GameData;

namespace Universalis.Application.Common;

public class WorldToDcRegion : IWorldToDcRegion
{
    private readonly IGameDataProvider _gdp;

    public WorldToDcRegion(IGameDataProvider gameDataProvider)
    {
        _gdp = gameDataProvider;
    }

    public (string Dc, string Region) Get(int worldId)
    {
        return _gdp.DataCenters().Where(d => d.WorldIds.Contains(worldId)).Select(d => (d.Name, d.Region)).First();
    }
}
