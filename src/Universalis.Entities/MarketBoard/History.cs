using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Universalis.Entities.MarketBoard
{
    public class History
    {
        [BsonElement("itemID")]
        public uint ItemId { get; init; }

        [BsonElement("worldID")]
        public uint WorldId { get; init; }

        [BsonElement("lastUploadTime")]
        public double LastUploadTimeUnixMilliseconds { get; set; }

        [BsonElement("entries")]
        public List<MinimizedSale> Sales { get; set; }
    }
}