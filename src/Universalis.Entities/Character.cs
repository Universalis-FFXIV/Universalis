namespace Universalis.Entities;

public class Character
{
    public string ContentIdSha256 { get; init; }

    public string Name { get; init; }

    public int? WorldId { get; init; }

    public Character()
    {
    }

    public Character(string contentIdSha256, string name, int? worldId)
    {
        ContentIdSha256 = contentIdSha256;
        Name = name;
        WorldId = worldId;
    }
}