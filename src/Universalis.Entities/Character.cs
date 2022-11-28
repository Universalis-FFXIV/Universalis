using Amazon.DynamoDBv2.DataModel;

namespace Universalis.Entities;

[DynamoDBTable("character")]
public class Character
{
    [DynamoDBHashKey("content_id_sha256")]
    public string ContentIdSha256 { get; }

    [DynamoDBProperty]
    public string Name { get; }

    [DynamoDBProperty]
    public uint? WorldId { get; }

    public Character(string contentIdSha256, string name, uint? worldId)
    {
        ContentIdSha256 = contentIdSha256;
        Name = name;
        WorldId = worldId;
    }
}