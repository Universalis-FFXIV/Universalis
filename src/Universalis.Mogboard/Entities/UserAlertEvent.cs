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
        var alertId = AlertId?.ToString() ?? "NULL";
        command.CommandText = "insert into @table (@id, @alertId, @userId, @added, @data)";
        command.Parameters.Add("@table", MySqlDbType.String);
        command.Parameters["@table"].Value = table;
        command.Parameters.Add("@id", MySqlDbType.VarChar);
        command.Parameters["@id"].Value = Id.ToString();
        command.Parameters.Add("@alertId", MySqlDbType.VarChar);
        command.Parameters["@alertId"].Value = alertId;
        command.Parameters.Add("@userId", MySqlDbType.VarChar);
        command.Parameters["@userId"].Value = UserId.ToString();
        command.Parameters.Add("@added", MySqlDbType.Int64);
        command.Parameters["@added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@data", MySqlDbType.VarChar);
        command.Parameters["@data"].Value = Data;
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