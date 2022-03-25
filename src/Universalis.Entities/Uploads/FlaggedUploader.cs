using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class FlaggedUploader
{
    [BsonElement("uploaderID")]
    public string UploaderIdHash { get; init; }
}