using MongoDB.Bson.Serialization.Attributes;

namespace Universalis.Entities;

public class Content
{
    [BsonElement("contentID")]
    public string ContentId { get; init; }

    [BsonElement("contentType")]
    public string ContentType { get; init; }

    [BsonElement("characterName")]
    public string CharacterName { get; init; }
}