using System;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard;

public class Listing
{
    public string ListingId { get; set; }

    public bool Hq { get; set; }

    public bool OnMannequin { get; set; }

    public List<Materia> Materia { get; set; }

    public int PricePerUnit { get; set; }

    public int Quantity { get; set; }

    public int DyeId { get; set; }

    public string CreatorId { get; set; }

    public string CreatorName { get; set; }

    public DateTime LastReviewTime { get; set; }

    public string RetainerId { get; set; }

    public string RetainerName { get; set; }

    public int RetainerCityId { get; set; }

    public string SellerId { get; set; }
    
    public int ItemId { get; set; }
    
    public int WorldId { get; set; }
    
    public bool Live { get; set; }
}