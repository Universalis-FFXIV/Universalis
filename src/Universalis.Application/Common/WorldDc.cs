using System;
using System.Linq;
using Universalis.GameData;

namespace Universalis.Application.Common
{
    public class WorldDc
    {
        public bool IsWorld { get; private init; }

        public uint WorldId { get; private init; }

        public string WorldName { get; private init; }

        public bool IsDc { get; private init; }

        public string DcName { get; private init; }

        /// <summary>
        /// Parses out a <see cref="WorldDc"/> from a string containing either a world name, a world ID, or a DC name.
        /// </summary>
        /// <param name="worldOrDc">The input string.</param>
        /// <param name="gameData">A game data provider.</param>
        /// <returns>A <see cref="WorldDc"/> object with either the world or the DC populated.</returns>
        public static WorldDc From(string worldOrDc, IGameDataProvider gameData)
        {
            if (worldOrDc == null) throw new NullReferenceException(nameof(worldOrDc));

            string dcName = null;
            _ = uint.TryParse(worldOrDc, out var worldId);
            if (worldId == default)
            {
                var cleanWorldOrDc = char.ToUpperInvariant(worldOrDc[0]) + worldOrDc[1..].ToLowerInvariant();

                // Effectively does nothing if the input doesn't refer to a Chinese world or DC
                cleanWorldOrDc = ChineseServers.RomanizedToHanzi(cleanWorldOrDc);

                _ = gameData.AvailableWorldsReversed().TryGetValue(cleanWorldOrDc, out worldId);
                if (worldId == default)
                {
                    if (!gameData.DataCenters().Select(dc => dc.Name).Contains(cleanWorldOrDc))
                    {
                        throw new ArgumentException("No world or DC matching the arguments was found.");
                    }

                    dcName = cleanWorldOrDc;
                }
            }

            return new WorldDc
            {
                IsWorld = worldId != default,
                WorldId = worldId,
                WorldName = worldId != default ? gameData.AvailableWorlds()[worldId] : null,
                IsDc = dcName != null,
                DcName = dcName,
            };
        }
    }
}