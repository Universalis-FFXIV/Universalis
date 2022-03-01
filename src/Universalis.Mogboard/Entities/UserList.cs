namespace Universalis.Mogboard.Entities;

public class UserList
{
    public string? Id { get; set; }

    public string? UserId { get; set; }

    public long Added { get; set; }

    public long Updated { get; set; }

    public string? Name { get; set; }

    public int Custom { get; set; }

    public int? CustomType { get; set; }

    public string? Items { get; set; }
}