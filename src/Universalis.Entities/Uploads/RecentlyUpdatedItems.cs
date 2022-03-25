using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class RecentlyUpdatedItems : ExtraData
{
    public static readonly string DefaultSetName = "recentlyUpdated";

    [BsonElement("items")]
    public List<uint> Items { get; set; }

    public RecentlyUpdatedItems() : base(DefaultSetName) { }
}