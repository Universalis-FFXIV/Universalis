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

    public void IntoCommand(MySqlCommand command, string table)
    {
        command.CommandText = "insert into @Table (@Id, @UserId, @Added, @Updated, @Name, @Custom, @CustomType, @Items)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@Added", MySqlDbType.Int64);
        command.Parameters["@Added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@Updated", MySqlDbType.Int64);
        command.Parameters["@Updated"].Value = Updated.ToUnixTimeSeconds();
        command.Parameters.Add("@Name", MySqlDbType.VarChar);
        command.Parameters["@Name"].Value = Name;
        command.Parameters.Add("@Custom", MySqlDbType.Bool);
        command.Parameters["@Custom"].Value = Custom;
        command.Parameters.Add("@CustomType", MySqlDbType.Int64);
        command.Parameters["@CustomType"].Value = CustomType;
        command.Parameters.Add("@Items", MySqlDbType.VarChar);
        command.Parameters["@Items"].Value = Items?.ToString();
    }

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