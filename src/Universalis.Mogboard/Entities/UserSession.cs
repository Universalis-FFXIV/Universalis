using MySqlConnector;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserSession
{
    public UserSessionId Id { get; set; }

    public UserId? UserId { get; set; }

    public string? Session { get; set; }

    public DateTimeOffset LastActive { get; set; }

    public string? Site { get; set; }

    public static UserSession FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        return new UserSession
        {
            Id = new UserSessionId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            Session = (string)reader["session"],
            LastActive = DateTimeOffset.FromUnixTimeSeconds((int)reader["last_active"]),
            Site = (string)reader["site"],
        };
    }
}