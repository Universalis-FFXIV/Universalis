using System.Collections.Generic;
using System.Collections.Immutable;

namespace Universalis.GameData
{
    public interface IGameDataProvider
    {
        /// <summary>
        /// Returns a read-only dictionary of all available worlds and world IDs.
        /// </summary>
        public IReadOnlyDictionary<uint, string> AvailableWorlds();

        /// <summary>
        /// Returns a sorted set of all marketable item IDs. This is useful for performing binary searches.
        /// </summary>
        public ImmutableSortedSet<uint> MarketableItems();
    }
}