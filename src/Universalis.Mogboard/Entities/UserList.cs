using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;

namespace Universalis.Mogboard.Entities;

[Table("users_lists")]
public class UserList
{
    [Column("id")]
    public UserListId Id { get; set; }

    [Column("user_id")]
    public UserId UserId { get; set; }

    [Column("added")]
    public int Added { get; set; }

    [Column("updated")]
    public int Updated { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("custom")]
    public bool Custom { get; set; }

    [Column("custom_type")]
    public int? CustomType { get; set; }

    [Column("items")]
    public string? Items { get; set; }

    public static UserList FromReader(MySqlDataReader reader)
    {
        return new UserList
        {
            Id = new UserListId((Guid)reader["id"]),
            UserId = new UserId((Guid)reader["user_id"]),
            Added = (int)reader["added"],
            Updated = (int)reader["updated"],
            Name = (string)reader["name"],
            Custom = (bool)reader["custom"],
            CustomType = (int)reader["custom_type"],
            Items = (string)reader["items"],
        };
    }
}