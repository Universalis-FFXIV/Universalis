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
    
    public void IntoCommand(MySqlCommand command, string table)
    {
        var userId = UserId?.ToString() ?? "NULL";
        var name = Name ?? "NULL";
        var server = Server ?? "NULL";
        var avatar = Avatar ?? "NULL";
        command.CommandText = "insert into @table (@id, @userId, @lodestoneId, @name, @server, @avatar, @main, @confirmed, @updated)";
        command.Parameters.Add("@table", MySqlDbType.String);
        command.Parameters["@table"].Value = table;
        command.Parameters.Add("@id", MySqlDbType.VarChar);
        command.Parameters["@id"].Value = Id.ToString();
        command.Parameters.Add("@userId", MySqlDbType.VarChar);
        command.Parameters["@userId"].Value = userId;
        command.Parameters.Add("@lodestoneId", MySqlDbType.Int64);
        command.Parameters["@lodestoneId"].Value = LodestoneId;
        command.Parameters.Add("@name", MySqlDbType.VarChar);
        command.Parameters["@name"].Value = name;
        command.Parameters.Add("@server", MySqlDbType.VarChar);
        command.Parameters["@server"].Value = server;
        command.Parameters.Add("@avatar", MySqlDbType.VarChar);
        command.Parameters["@avatar"].Value = avatar;
        command.Parameters.Add("@main", MySqlDbType.Int64);
        command.Parameters["@main"].Value = Main;
        command.Parameters.Add("@confirmed", MySqlDbType.Int64);
        command.Parameters["@confirmed"].Value = Confirmed;
        command.Parameters.Add("@updated", MySqlDbType.Int64);
        command.Parameters["@updated"].Value = Updated.ToUnixTimeSeconds();
    }

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