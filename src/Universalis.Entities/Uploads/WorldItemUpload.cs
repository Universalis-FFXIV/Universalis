using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class WorldItemUpload
    {
        [BsonElement("itemID")]
        public uint ItemId { get; init; }

        [BsonElement("worldID")]
        public uint WorldId { get; init; }

        [BsonElement("lastUploadTime")]
        public double LastUploadTimeUnixMilliseconds { get; init; }
    }
}