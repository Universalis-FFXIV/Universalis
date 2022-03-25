using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class TrustedSource
{
    [BsonElement("sourceName")]
    public string Name { get; init; }

    [BsonElement("apiKey")]
    public string ApiKeySha512 { get; init; } // There's no real reason for this to be hashed, but ~legacy~

    [BsonElement("uploadCount")]
    public double UploadCount { get; set; }
}