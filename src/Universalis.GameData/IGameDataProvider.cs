using System.Collections.Generic;

namespace Universalis.GameData
{
    public interface IGameDataProvider
    {
        /// <summary>
        /// Returns a read-only dictionary of all available worlds and world IDs.
        /// </summary>
        public IReadOnlyDictionary<uint, string> AvailableWorlds();

        /// <summary>
        /// Returns a read-only dictionary of all available world IDs and worlds.
        /// </summary>
        public IReadOnlyDictionary<string, uint> AvailableWorldsReversed();

        /// <summary>
        /// Returns a sorted set of all available world IDs. This is useful for performing binary searches.
        /// </summary>
        public IReadOnlySet<uint> AvailableWorldIds();

        /// <summary>
        /// Returns a sorted set of all marketable item IDs. This is useful for performing binary searches.
        /// </summary>
        public IReadOnlySet<uint> MarketableItemIds();

        /// <summary>
        /// Returns a sorted set of all data centers.
        /// </summary>
        public IEnumerable<DataCenter> DataCenters();
    }
}