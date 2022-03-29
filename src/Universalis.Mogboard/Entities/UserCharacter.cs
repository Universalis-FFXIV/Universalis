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
        command.CommandText = "insert into @Table (@Id, @UserId, @LodestoneId, @Name, @Server, @Avatar, @Main, @Confirmed, @Updated)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@LodestoneId", MySqlDbType.Int64);
        command.Parameters["@LodestoneId"].Value = LodestoneId;
        command.Parameters.Add("@Name", MySqlDbType.VarChar);
        command.Parameters["@Name"].Value = Name;
        command.Parameters.Add("@Server", MySqlDbType.VarChar);
        command.Parameters["@Server"].Value = Server;
        command.Parameters.Add("@Avatar", MySqlDbType.VarChar);
        command.Parameters["@Avatar"].Value = Avatar;
        command.Parameters.Add("@Main", MySqlDbType.Bool);
        command.Parameters["@Main"].Value = Main;
        command.Parameters.Add("@Confirmed", MySqlDbType.Bool);
        command.Parameters["@Confirmed"].Value = Confirmed;
        command.Parameters.Add("@Updated", MySqlDbType.Int64);
        command.Parameters["@Updated"].Value = Updated.ToUnixTimeSeconds();
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