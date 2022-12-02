using System.Collections.Generic;

namespace Universalis.GameData;

public interface IGameDataProvider
{
    /// <summary>
    /// Returns a read-only dictionary of all available worlds and world IDs.
    /// </summary>
    IReadOnlyDictionary<int, string> AvailableWorlds();

    /// <summary>
    /// Returns a read-only dictionary of all available world IDs and worlds.
    /// </summary>
    IReadOnlyDictionary<string, int> AvailableWorldsReversed();

    /// <summary>
    /// Returns a sorted set of all available world IDs. This is useful for performing binary searches.
    /// </summary>
    IReadOnlySet<int> AvailableWorldIds();

    /// <summary>
    /// Returns a sorted set of all marketable item IDs. This is useful for performing binary searches.
    /// </summary>
    IReadOnlySet<int> MarketableItemIds();

    /// <summary>
    /// Returns a read-only dictionary of the stack size limits for all marketable items.
    /// </summary>
    IReadOnlyDictionary<int, int> MarketableItemStackSizes();

    /// <summary>
    /// Returns a sorted set of all data centers.
    /// </summary>
    IEnumerable<DataCenter> DataCenters();
}