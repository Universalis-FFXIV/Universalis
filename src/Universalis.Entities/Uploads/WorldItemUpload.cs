using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities.Uploads;

public class WorldItemUpload
{
    [BsonElement("itemID")]
    public int ItemId { get; init; }

    [BsonElement("worldID")]
    public int WorldId { get; init; }

    [BsonElement("lastUploadTime")]
    public double LastUploadTimeUnixMilliseconds { get; init; }
}