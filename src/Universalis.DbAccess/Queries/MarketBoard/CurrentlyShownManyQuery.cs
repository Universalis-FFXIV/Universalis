using System.Collections.Generic;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class CurrentlyShownManyQuery
{
    public IEnumerable<int> WorldIds { get; init; }

    public IEnumerable<int> ItemIds { get; init; }
}