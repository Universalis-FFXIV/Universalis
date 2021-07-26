using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploaders
{
    public class TrustedSource
    {
        [BsonElement("sourceName")]
        public string Name { get; set; }

        [BsonElement("apiKey")]
        public string ApiKeySha256 { get; set; } // There's no real reason for this to be hashed, but ~legacy~

        [BsonElement("uploadCount")]
        public uint UploadCount { get; set; }
    }
}