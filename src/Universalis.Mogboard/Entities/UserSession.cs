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

    public void IntoCommand(MySqlCommand command, string table)
    {
        command.CommandText = "insert into @Table (@Id, @UserId, @Session, @LastActive, @Site)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@Session", MySqlDbType.VarChar);
        command.Parameters["@Session"].Value = Session;
        command.Parameters.Add("@LastActive", MySqlDbType.Int64);
        command.Parameters["@LastActive"].Value = LastActive.ToUnixTimeSeconds();
        command.Parameters.Add("@Site", MySqlDbType.VarChar);
        command.Parameters["@Site"].Value = Site;
    }

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