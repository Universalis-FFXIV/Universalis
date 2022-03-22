using MySqlConnector;
using Universalis.Mogboard.Doctrine;

namespace Universalis.Mogboard.Entities;

public class UserList
{
    public UserListId Id { get; set; }
    
    public UserId UserId { get; set; }
    
    public int Added { get; set; }

    public int Updated { get; set; }
    
    public string? Name { get; set; }
    
    public bool Custom { get; set; }
    
    public int? CustomType { get; set; }
    
    public DoctrineArray<int>? Items { get; set; }

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
            Items = DoctrineArray<int>.Parse((string)reader["items"]),
        };
    }
}