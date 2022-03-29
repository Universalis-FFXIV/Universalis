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
    
    public void IntoCommand(MySqlCommand command, string table)
    {
        var userId = UserId?.ToString() ?? "NULL";
        command.CommandText = "insert into @table (@id, @userId, @uniq, @itemId, @added, @activeTime, @lastChecked, @name, " +
                              "@server, @expiry, @triggerConditions, @triggerType, @triggerLastSent, @triggersSent, @triggerAction, " +
                              "@triggerDataCenter, @triggerHq, @triggerNq, @triggerActive, @notifiedViaEmail, @notifiedViaDiscord, " +
                              "@keepUpdated)";
        command.Parameters.Add("@table", MySqlDbType.String);
        command.Parameters["@table"].Value = table;
        command.Parameters.Add("@id", MySqlDbType.VarChar);
        command.Parameters["@id"].Value = Id.ToString();
        command.Parameters.Add("@userId", MySqlDbType.VarChar);
        command.Parameters["@userId"].Value = userId;
        command.Parameters.Add("@itemId", MySqlDbType.Int64);
        command.Parameters["@itemId"].Value = ItemId;
        command.Parameters.Add("@added", MySqlDbType.Int64);
        command.Parameters["@added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@activeTime", MySqlDbType.Int64);
        command.Parameters["@activeTime"].Value = ActiveTime.ToUnixTimeSeconds();
        command.Parameters.Add("@lastChecked", MySqlDbType.Int64);
        command.Parameters["@lastChecked"].Value = LastChecked.ToUnixTimeSeconds();
        command.Parameters.Add("@name", MySqlDbType.VarChar);
        command.Parameters["@name"].Value = Name;
        command.Parameters.Add("@server", MySqlDbType.VarChar);
        command.Parameters["@server"].Value = Server;
        command.Parameters.Add("@expiry", MySqlDbType.Int64);
        command.Parameters["@expiry"].Value = Expiry.ToUnixTimeSeconds();
        command.Parameters.Add("@triggerConditions", MySqlDbType.VarChar);
        command.Parameters["@triggerConditions"].Value = TriggerConditions;
        command.Parameters.Add("@triggerType", MySqlDbType.VarChar);
        command.Parameters["@triggerType"].Value = TriggerType;
        command.Parameters.Add("@triggerLastSent", MySqlDbType.Int64);
        command.Parameters["@triggerLastSent"].Value = TriggerLastSent.ToUnixTimeSeconds();
        command.Parameters.Add("@triggersSent", MySqlDbType.Int64);
        command.Parameters["@triggersSent"].Value = TriggersSent;
        command.Parameters.Add("@triggerAction", MySqlDbType.VarChar);
        command.Parameters["@triggerAction"].Value = TriggerAction;
        command.Parameters.Add("@triggerDataCenter", MySqlDbType.Int64);
        command.Parameters["@triggerDataCenter"].Value = TriggerDataCenter;
        command.Parameters.Add("@triggerHq", MySqlDbType.Int64);
        command.Parameters["@triggerHq"].Value = TriggerHq;
        command.Parameters.Add("@triggerNq", MySqlDbType.Int64);
        command.Parameters["@triggerNq"].Value = TriggerNq;
        command.Parameters.Add("@triggerActive", MySqlDbType.Int64);
        command.Parameters["@triggerActive"].Value = TriggerActive;
        command.Parameters.Add("@notifiedViaEmail", MySqlDbType.Int64);
        command.Parameters["@notifiedViaEmail"].Value = NotifiedViaEmail;
        command.Parameters.Add("@notifiedViaDiscord", MySqlDbType.Int64);
        command.Parameters["@notifiedViaDiscord"].Value = NotifiedViaDiscord;
        command.Parameters.Add("@keepUpdated", MySqlDbType.Int64);
        command.Parameters["@keepUpdated"].Value = KeepUpdated;
    }

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