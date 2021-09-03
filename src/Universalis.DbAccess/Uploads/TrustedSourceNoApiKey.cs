using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.DbAccess.Uploads
{
    public class TrustedSourceNoApiKey
    {
        [BsonElement("sourceName")]
        public string Name { get; init; }
        
        [BsonElement("uploadCount")]
        public double UploadCount { get; init; }
    }
}