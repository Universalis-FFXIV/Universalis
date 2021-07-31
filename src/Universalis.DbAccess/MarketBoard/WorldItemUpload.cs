using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.DbAccess.MarketBoard
{
    public class WorldItemUpload
    {
        [BsonElement("itemID")]
        public uint ItemId { get; init; }

        [BsonElement("worldID")]
        public uint WorldId { get; init; }

        [BsonElement("lastUploadTime")]
        public uint LastUploadTimeUnixMilliseconds { get; init; }
    }
}