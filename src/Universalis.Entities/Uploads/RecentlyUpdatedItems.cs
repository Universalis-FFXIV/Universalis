using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class RecentlyUpdatedItems : ExtraData
    {
        public const string DefaultSetName = "recentlyUpdated";

        [BsonElement("items")]
        public List<uint> Items { get; init; }

        public RecentlyUpdatedItems() : base(DefaultSetName) { }
    }
}