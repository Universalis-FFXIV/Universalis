using System.Collections.Generic;

namespace Universalis.DbAccess.Queries.Uploads;

public class MostRecentlyUpdatedManyQuery
{
    public int[] WorldIds { get; init; }

    public int Count { get; init; }

    /// <summary>
    /// Set of valid item IDs to filter by during heap merge.
    /// Only items with IDs in this set are included in the merged results.
    /// This prevents non-marketable items (stale data) from dominating the heap
    /// and crowding out valid items, especially in DC/region queries.
    /// </summary>
    public IReadOnlySet<int> ValidItemIds { get; init; }
}
