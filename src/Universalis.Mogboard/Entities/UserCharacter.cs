using MySqlConnector;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserCharacter
{
    public UserCharacterId Id { get; set; }

    public UserId? UserId { get; set; }

    public int LodestoneId { get; set; }

    public string? Name { get; set; }

    public string? Server { get; set; }

    public string? Avatar { get; set; }

    public bool Main { get; set; }

    public bool Confirmed { get; set; }

    public DateTimeOffset Updated { get; set; }

    public static UserCharacter FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        var name = reader["name"];
        var server = reader["server"];
        var avatar = reader["avatar"];
        return new UserCharacter
        {
            Id = new UserCharacterId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            LodestoneId = (int)reader["lodestone_id"],
            Name = (string?)(name == DBNull.Value ? null : name),
            Server = (string?)(server == DBNull.Value ? null : server),
            Avatar = (string?)(avatar == DBNull.Value ? null : avatar),
            Main = (bool)reader["main"],
            Confirmed = (bool)reader["confirmed"],
            Updated = DateTimeOffset.FromUnixTimeSeconds((int)reader["updated"]),
        };
    }
}