namespace Universalis.Entities;

public class Character
{
    public string ContentIdSha256 { get; }
    
    public string Name { get; }
    
    public uint? WorldId { get; }

    public Character(string contentIdSha256, string name, uint? worldId)
    {
        ContentIdSha256 = contentIdSha256;
        Name = name;
        WorldId = worldId;
    }
}