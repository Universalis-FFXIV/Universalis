using System.Collections.Generic;

namespace Universalis.GameData;

public interface IGameDataProvider
{
    /// <summary>
    /// Returns a read-only dictionary of all available worlds and world IDs.
    /// </summary>
    IReadOnlyDictionary<uint, string> AvailableWorlds();

    /// <summary>
    /// Returns a read-only dictionary of all available world IDs and worlds.
    /// </summary>
    IReadOnlyDictionary<string, uint> AvailableWorldsReversed();

    /// <summary>
    /// Returns a sorted set of all available world IDs. This is useful for performing binary searches.
    /// </summary>
    IReadOnlySet<uint> AvailableWorldIds();

    /// <summary>
    /// Returns a sorted set of all marketable item IDs. This is useful for performing binary searches.
    /// </summary>
    IReadOnlySet<uint> MarketableItemIds();

    /// <summary>
    /// Returns a read-only dictionary of the stack size limits for all marketable items.
    /// </summary>
    IReadOnlyDictionary<uint, uint> MarketableItemStackSizes();

    /// <summary>
    /// Returns a sorted set of all data centers.
    /// </summary>
    IEnumerable<DataCenter> DataCenters();
}