using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploaders
{
    public class WorldUploadCount : ExtraData
    {
        [BsonElement("count")]
        public uint Count { get; set; }

        [BsonElement("worldName")]
        public string WorldName { get; set; }
    }
}