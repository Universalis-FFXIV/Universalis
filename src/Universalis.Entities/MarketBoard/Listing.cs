using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class Listing
{
    public string ListingId { get; init; }

    public bool Hq { get; init; }

    public bool OnMannequin { get; init; }

    public List<Materia> Materia { get; init; }

    public uint PricePerUnit { get; set; }

    public uint Quantity { get; init; }

    public uint DyeId { get; init; }

    public string CreatorId { get; init; }

    public string CreatorName { get; init; }

    public long LastReviewTimeUnixSeconds { get; init; }

    public string RetainerId { get; init; }

    public string RetainerName { get; init; }

    public int RetainerCityId { get; init; }

    public string SellerId { get; init; }
}