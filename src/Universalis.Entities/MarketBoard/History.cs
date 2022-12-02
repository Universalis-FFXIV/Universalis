using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class History
{
    public int ItemId { get; init; }

    public int WorldId { get; init; }

    public double LastUploadTimeUnixMilliseconds { get; set; }

    public List<Sale> Sales { get; set; }
}