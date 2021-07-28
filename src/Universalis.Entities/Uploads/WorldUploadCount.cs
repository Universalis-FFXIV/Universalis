using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class WorldUploadCount : ExtraData
    {
        [BsonElement("count")]
        public uint Count { get; init; }

        [BsonElement("worldName")]
        public string WorldName { get; init; }
    }
}