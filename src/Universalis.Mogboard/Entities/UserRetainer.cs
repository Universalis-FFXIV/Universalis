using MySqlConnector;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserRetainer
{
    public UserRetainerId Id { get; set; }

    public UserId? UserId { get; set; }

    public string? Name { get; set; }

    public string? Server { get; set; }

    public string? Avatar { get; set; }

    public bool Confirmed { get; set; }

    public int ConfirmItem { get; set; }

    public int ConfirmPrice { get; set; }

    public DateTimeOffset Updated { get; set; }

    public DateTimeOffset Added { get; set; }

    public string? ApiRetainerId { get; set; }

    public void IntoCommand(MySqlCommand command, string table)
    {
        command.CommandText = "insert into @Table (@Id, @UserId, @Name, @Server, @Avatar, @Confirmed, @ConfirmItem, @ConfirmPrice, @Updated, @Added, @ApiRetainerId)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@Name", MySqlDbType.VarChar);
        command.Parameters["@Name"].Value = Name;
        command.Parameters.Add("@Server", MySqlDbType.VarChar);
        command.Parameters["@Server"].Value = Server;
        command.Parameters.Add("@Avatar", MySqlDbType.VarChar);
        command.Parameters["@Avatar"].Value = Avatar;
        command.Parameters.Add("@Confirmed", MySqlDbType.Bool);
        command.Parameters["@Confirmed"].Value = Confirmed;
        command.Parameters.Add("@ConfirmItem", MySqlDbType.Int64);
        command.Parameters["@ConfirmItem"].Value = ConfirmItem;
        command.Parameters.Add("@ConfirmPrice", MySqlDbType.Int64);
        command.Parameters["@ConfirmPrice"].Value = ConfirmPrice;
        command.Parameters.Add("@Updated", MySqlDbType.Int64);
        command.Parameters["@Updated"].Value = Updated.ToUnixTimeSeconds();
        command.Parameters.Add("@Added", MySqlDbType.Int64);
        command.Parameters["@Added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@ApiRetainerId", MySqlDbType.VarChar);
        command.Parameters["@ApiRetainerId"].Value = ApiRetainerId;
    }

    public static UserRetainer FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        var name = reader["name"];
        var server = reader["server"];
        var avatar = reader["avatar"];
        var apiRetainerId = reader["api_retainer_id"];
        return new UserRetainer
        {
            Id = new UserRetainerId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            Name = (string?)(name == DBNull.Value ? null : name),
            Server = (string?)(server == DBNull.Value ? null : server),
            Avatar = (string?)(avatar == DBNull.Value ? null : avatar),
            Confirmed = (bool)reader["confirmed"],
            ConfirmItem = (int)reader["confirm_item"],
            ConfirmPrice = (int)reader["confirm_price"],
            Updated = DateTimeOffset.FromUnixTimeSeconds((int)reader["updated"]),
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            ApiRetainerId = (string?)(apiRetainerId == DBNull.Value ? null : apiRetainerId),
        };
    }
}