using MySqlConnector;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserAlertEvent
{
    public UserAlertEventId Id { get; set; }

    public UserAlertId? AlertId { get; set; }

    public UserId UserId { get; set; }

    public DateTimeOffset Added { get; set; }

    public string? Data { get; set; }

    public void IntoCommand(MySqlCommand command, string table)
    {
        command.CommandText = "insert into @Table (@Id, @AlertId, @UserId, @Added, @Data)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@AlertId", MySqlDbType.VarChar);
        command.Parameters["@AlertId"].Value = AlertId?.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId.ToString();
        command.Parameters.Add("@Added", MySqlDbType.Int64);
        command.Parameters["@Added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@data", MySqlDbType.VarChar);
        command.Parameters["@Data"].Value = Data;
    }

    public static UserAlertEvent FromReader(MySqlDataReader reader)
    {
        var alertId = reader["event_id"];
        return new UserAlertEvent
        {
            Id = new UserAlertEventId((Guid)reader["id"]),
            AlertId = alertId == DBNull.Value ? null : new UserAlertId((Guid)alertId),
            UserId = UserId.Parse((string)reader["user_id"]),
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            Data = (string)reader["data"],
        };
    }
}