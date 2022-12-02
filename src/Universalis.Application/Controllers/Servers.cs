using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

[TypeConverter(typeof(ServersConverter))]
public class Servers
{
    private readonly List<string> _names = new();

    private readonly List<int> _worldIds = new();

    private void AddServerName(string serverName)
    {
        _names.Add(serverName);
    }

    private void AddWorldId(int worldId)
    {
        _worldIds.Add(worldId);
    }

    public bool TryResolveWorlds(IGameDataProvider gameData, out IEnumerable<World> worlds)
    {
        worlds = null;
        
        var result = new List<World>();

        foreach (var worldId in _worldIds)
        {
            if (!gameData.AvailableWorldIds().Contains(worldId))
            {
                return false;
            }

            result.Add(new World { Id = worldId, Name = gameData.AvailableWorlds()[worldId] });
        }

        foreach (var name in _names)
        {
            if (gameData.AvailableWorldsReversed().ContainsKey(name))
            {
                result.Add(new World { Id = gameData.AvailableWorldsReversed()[name], Name = name });
            }
            else if (gameData.DataCenters().Any(dc => dc.Name == name))
            {
                var dc = gameData.DataCenters().First(dc => dc.Name == name);
                result.AddRange(dc.WorldIds.Select(worldId => new World
                    { Id = worldId, Name = gameData.AvailableWorlds()[worldId] }));
            }
            else
            {
                return false;
            }
        }

        worlds = result;
        return true;
    }

    public static bool TryParse(string s, out Servers servers)
    {
        servers = new Servers();

        var parts = s.Split(',');
        foreach (var part in parts)
        {
            if (int.TryParse(part, out var id))
            {
                servers.AddWorldId(id);
            }
            else
            {
                var cleanName = (char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()).Normalize();
                servers.AddServerName(cleanName);
            }
        }

        return true;
    }
}