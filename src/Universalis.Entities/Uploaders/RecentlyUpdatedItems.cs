using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploaders
{
    public class RecentlyUpdatedItems : ExtraData
    {
        [BsonElement("items")]
        public List<uint> Items { get; init; }
    }
}