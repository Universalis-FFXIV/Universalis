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
        command.CommandText = "insert into @Table (@Id, @UserId, @Uniq, @ItemId, @Added, @ActiveTime, @LastChecked, @Name, " +
                              "@Server, @Expiry, @TriggerConditions, @TriggerType, @TriggerLastSent, @TriggersSent, @TriggerAction, " +
                              "@TriggerDataCenter, @TriggerHq, @TriggerNq, @TriggerActive, @NotifiedViaEmail, @NotifiedViaDiscord, " +
                              "@KeepUpdated)";
        command.Parameters.Add("@Table", MySqlDbType.String);
        command.Parameters["@Table"].Value = table;
        command.Parameters.Add("@Id", MySqlDbType.VarChar);
        command.Parameters["@Id"].Value = Id.ToString();
        command.Parameters.Add("@UserId", MySqlDbType.VarChar);
        command.Parameters["@UserId"].Value = UserId?.ToString();
        command.Parameters.Add("@Uniq", MySqlDbType.VarChar);
        command.Parameters["@Uniq"].Value = Uniq;
        command.Parameters.Add("@ItemId", MySqlDbType.Int64);
        command.Parameters["@ItemId"].Value = ItemId;
        command.Parameters.Add("@Added", MySqlDbType.Int64);
        command.Parameters["@Added"].Value = Added.ToUnixTimeSeconds();
        command.Parameters.Add("@ActiveTime", MySqlDbType.Int64);
        command.Parameters["@ActiveTime"].Value = ActiveTime.ToUnixTimeSeconds();
        command.Parameters.Add("@LastChecked", MySqlDbType.Int64);
        command.Parameters["@LastChecked"].Value = LastChecked.ToUnixTimeSeconds();
        command.Parameters.Add("@Name", MySqlDbType.VarChar);
        command.Parameters["@Name"].Value = Name;
        command.Parameters.Add("@Server", MySqlDbType.VarChar);
        command.Parameters["@Server"].Value = Server;
        command.Parameters.Add("@Expiry", MySqlDbType.Int64);
        command.Parameters["@Expiry"].Value = Expiry.ToUnixTimeSeconds();
        command.Parameters.Add("@TriggerConditions", MySqlDbType.VarChar);
        command.Parameters["@TriggerConditions"].Value = TriggerConditions;
        command.Parameters.Add("@TriggerType", MySqlDbType.VarChar);
        command.Parameters["@TriggerType"].Value = TriggerType;
        command.Parameters.Add("@TriggerLastSent", MySqlDbType.Int64);
        command.Parameters["@TriggerLastSent"].Value = TriggerLastSent.ToUnixTimeSeconds();
        command.Parameters.Add("@TriggersSent", MySqlDbType.Int64);
        command.Parameters["@TriggersSent"].Value = TriggersSent;
        command.Parameters.Add("@TriggerAction", MySqlDbType.VarChar);
        command.Parameters["@TriggerAction"].Value = TriggerAction;
        command.Parameters.Add("@TriggerDataCenter", MySqlDbType.Bool);
        command.Parameters["@TriggerDataCenter"].Value = TriggerDataCenter;
        command.Parameters.Add("@TriggerHq", MySqlDbType.Bool);
        command.Parameters["@TriggerHq"].Value = TriggerHq;
        command.Parameters.Add("@TriggerNq", MySqlDbType.Bool);
        command.Parameters["@TriggerNq"].Value = TriggerNq;
        command.Parameters.Add("@TriggerActive", MySqlDbType.Bool);
        command.Parameters["@TriggerActive"].Value = TriggerActive;
        command.Parameters.Add("@NotifiedViaEmail", MySqlDbType.Bool);
        command.Parameters["@NotifiedViaEmail"].Value = NotifiedViaEmail;
        command.Parameters.Add("@NotifiedViaDiscord", MySqlDbType.Bool);
        command.Parameters["@NotifiedViaDiscord"].Value = NotifiedViaDiscord;
        command.Parameters.Add("@KeepUpdated", MySqlDbType.Bool);
        command.Parameters["@KeepUpdated"].Value = KeepUpdated;
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