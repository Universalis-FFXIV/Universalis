using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class Listing
{
    public string ListingId { get; set; }

    public bool Hq { get; set; }

    public bool OnMannequin { get; set; }

    public List<Materia> Materia { get; set; }

    public uint PricePerUnit { get; set; }

    public uint Quantity { get; set; }

    public uint DyeId { get; set; }

    public string CreatorId { get; set; }

    public string CreatorName { get; set; }

    public long LastReviewTimeUnixSeconds { get; set; }

    public string RetainerId { get; set; }

    public string RetainerName { get; set; }

    public int RetainerCityId { get; set; }

    public string SellerId { get; set; }
}