using MySqlConnector;
using Universalis.Mogboard.Doctrine;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserReport
{
    public UserReportId Id { get; set; }

    public UserId? UserId { get; set; }

    public DateTimeOffset Added { get; set; }

    public string? Name { get; set; }

    public DoctrineArray<int>? Items { get; set; }

    public void IntoCommand(MySqlCommand command, string table)
    {
        command.CommandText = "insert into @Table (@Id, @UserId, @Added, @Name, @Items)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@Added", MySqlDbType.Int64);
        command.Parameters["@Added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@Name", MySqlDbType.VarChar);
        command.Parameters["@Name"].Value = Name;
        command.Parameters.Add("@Items", MySqlDbType.VarChar);
        command.Parameters["@Items"].Value = Items?.ToString();
    }

    public static UserReport FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        return new UserReport
        {
            Id = new UserReportId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            Name = (string)reader["name"],
            Items = DoctrineArray<int>.Parse((string)reader["items"]),
        };
    }
}