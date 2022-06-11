using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.DbAccess.AccessControl;

public class TrustedSourceNoApiKey
{
    [BsonElement("sourceName")]
    public string Name { get; init; }
        
    [BsonElement("uploadCount")]
    public double UploadCount { get; init; }
}