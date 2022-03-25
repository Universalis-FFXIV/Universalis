using MySqlConnector;
using Universalis.Mogboard.Doctrine;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class UserAlert
{
    public UserAlertId Id { get; set; }

    public UserId? UserId { get; set; }

    public string? Uniq { get; set; }

    public int ItemId { get; set; }

    public DateTimeOffset Added { get; set; }

    public DateTimeOffset ActiveTime { get; set; }

    public DateTimeOffset LastChecked { get; set; }

    public string? Name { get; set; }

    public string? Server { get; set; }

    public DateTimeOffset Expiry { get; set; }

    public DoctrineArray<string>? TriggerConditions { get; set; }

    public string? TriggerType { get; set; }

    public DateTimeOffset TriggerLastSent { get; set; }

    public int TriggersSent { get; set; }

    public string? TriggerAction { get; set; }

    public bool TriggerDataCenter { get; set; }

    public bool TriggerHq { get; set; }

    public bool TriggerNq { get; set; }

    public bool TriggerActive { get; set; }

    public bool NotifiedViaEmail { get; set; }

    public bool NotifiedViaDiscord { get; set; }

    public bool KeepUpdated { get; set; }

    public static UserAlert FromReader(MySqlDataReader reader)
    {
        var userId = reader["user_id"];
        return new UserAlert
        {
            Id = new UserAlertId((Guid)reader["id"]),
            UserId = userId == DBNull.Value ? null : new UserId((Guid)userId),
            Uniq = (string)reader["uniq"],
            ItemId = (int)reader["item_id"],
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            ActiveTime = DateTimeOffset.FromUnixTimeSeconds((int)reader["active_time"]),
            LastChecked = DateTimeOffset.FromUnixTimeSeconds((int)reader["last_checked"]),
            Name = (string)reader["name"],
            Server = (string)reader["server"],
            Expiry = DateTimeOffset.FromUnixTimeSeconds((int)reader["expiry"]),
            TriggerConditions = DoctrineArray<string>.Parse((string)reader["trigger_conditions"]),
            TriggerType = (string)reader["trigger_type"],
            TriggerLastSent = DateTimeOffset.FromUnixTimeSeconds((int)reader["trigger_last_sent"]),
            TriggersSent = (int)reader["triggers_sent"],
            TriggerAction = (string)reader["trigger_action"],
            TriggerDataCenter = (bool)reader["trigger_data_center"],
            TriggerHq = (bool)reader["trigger_hq"],
            TriggerNq = (bool)reader["trigger_nq"],
            TriggerActive = (bool)reader["trigger_active"],
            NotifiedViaEmail = (bool)reader["notified_via_email"],
            NotifiedViaDiscord = (bool)reader["notified_via_discord"],
            KeepUpdated = (bool)reader["keep_updated"],
        };
    }
}