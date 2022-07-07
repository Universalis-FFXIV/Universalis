using System.Linq;
using Universalis.GameData;

namespace Universalis.Application.Common;

public class WorldDcRegion
{
    public bool IsWorld { get; private init; }

    public uint WorldId { get; private init; }

    public string WorldName { get; private init; }

    public bool IsDc { get; private init; }

    public string DcName { get; private init; }

    public bool IsRegion { get; private init; }

    public string RegionName { get; private init; }

    /// <summary>
    /// Parses out a <see cref="WorldDcRegion"/> from a string containing either a world name, a world ID, a DC name, or a region name.
    /// </summary>
    /// <param name="worldOrDc">The input string.</param>
    /// <param name="gameData">A game data provider.</param>
    /// <param name="worldDcRegion">A <see cref="WorldDcRegion"/> object with either the world or the DC populated.</param>
    /// <returns>Whether or not parsing succeeded.</returns>
    public static bool TryParse(string worldOrDc, IGameDataProvider gameData, out WorldDcRegion worldDcRegion)
    {
        worldDcRegion = null;

        if (worldOrDc == null)
        {
            return false;
        }

        string worldName = null;
        string dcName = null;
        string regionName = null;
        var worldIdParsed = uint.TryParse(worldOrDc, out var worldId);
        if (!worldIdParsed)
        {
            var cleanText = string.Join('-',
                worldOrDc.Split('-').Select(term => char.ToUpperInvariant(term[0]) + term[1..].ToLowerInvariant()));

            // Effectively does nothing if the input doesn't refer to a Chinese world, DC, or region
            cleanText = ChineseServers.RomanizedToHanzi(cleanText);
            cleanText = ChineseServers.RegionToHanzi(cleanText);

            worldIdParsed = gameData.AvailableWorldsReversed().TryGetValue(cleanText, out worldId);

            if (!worldIdParsed)
            {
                if (!gameData.DataCenters().Select(dc => dc.Name).Contains(cleanText))
                {
                    if (!gameData.DataCenters().Select(dc => dc.Region.ToLowerInvariant())
                            .Contains(cleanText.ToLowerInvariant()))
                    {
                        return false;
                    }

                    regionName = cleanText;
                }
                else
                {
                    dcName = cleanText;
                }
            }
            else
            {
                worldName = cleanText;
            }
        }
        else if (!gameData.AvailableWorlds().TryGetValue(worldId, out worldName))
        {
            return false;
        }

        worldDcRegion = new WorldDcRegion
        {
            IsWorld = worldIdParsed,
            WorldId = worldId,
            WorldName = worldName,
            IsDc = dcName != null,
            DcName = dcName,
            IsRegion = regionName != null,
            RegionName = regionName,
        };

        return true;
    }
}