﻿using System.Collections.Generic;

namespace Universalis.DbAccess.Queries.MarketBoard;

public class HistoryManyQuery
{
    public IEnumerable<int> WorldIds { get; init; }

    public IEnumerable<int> ItemIds { get; init; }

    public int? Count { get; init; }
}