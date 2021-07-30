using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads
{
    public class WorldUploadCount : ExtraData
    {
        public static readonly string DefaultSetName = "worldUploadCount";

        [BsonElement("count")]
        public uint Count { get; init; }

        [BsonElement("worldName")]
        public string WorldName { get; init; }

        public WorldUploadCount() : base(DefaultSetName) { }
    }
}