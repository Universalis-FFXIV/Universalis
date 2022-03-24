using MySqlConnector;
using Universalis.Mogboard.Doctrine;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserList
{
    public UserListId Id { get; set; }
    
    public UserId? UserId { get; set; }
    
    public DateTimeOffset Added { get; set; }

    public DateTimeOffset Updated { get; set; }
    
    public string? Name { get; set; }
    
    public bool Custom { get; set; }
    
    public int? CustomType { get; set; }
    
    public DoctrineArray<int>? Items { get; set; }

    public static UserList FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        var customType = reader["custom_type"];
        return new UserList
        {
            Id = new UserListId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            Updated = DateTimeOffset.FromUnixTimeSeconds((int)reader["updated"]),
            Name = (string)reader["name"],
            Custom = (bool)reader["custom"],
            CustomType = (int?)(customType == DBNull.Value ? null : customType),
            Items = DoctrineArray<int>.Parse((string)reader["items"]),
        };
    }
}