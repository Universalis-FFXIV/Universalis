using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class History
{
    public uint ItemId { get; init; }

    public uint WorldId { get; init; }

    public double LastUploadTimeUnixMilliseconds { get; set; }

    public List<Sale> Sales { get; set; }
}