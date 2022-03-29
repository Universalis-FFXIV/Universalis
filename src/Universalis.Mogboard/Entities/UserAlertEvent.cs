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

    public string ToInsertStatement(string table)
    {
        var alertId = AlertId?.ToString() ?? "NULL";
        return $"insert into {table} ('{Id}', '{alertId}', '{UserId}', {Added.ToUnixTimeSeconds()}, '{Data}')";
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