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