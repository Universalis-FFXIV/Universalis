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