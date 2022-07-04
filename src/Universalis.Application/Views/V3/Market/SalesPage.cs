using System.Collections.Generic;

namespace Universalis.Application.Views.V3.Market;

public class SalesPage
{
    /// <summary>
    /// The sales on the current page. The number of sales that will be returned is not defined.
    /// </summary>
    public IList<Sale> Sales { get; init; }

    /// <summary>
    /// The cursor into the next page of sales. This will be null when there are no more sales to return.
    /// </summary>
    public string Next { get; init; }
}