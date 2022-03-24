using MySqlConnector;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Mogboard.Entities;

public class User
{
    public UserId Id { get; set; }

    public DateTimeOffset Added { get; set; }

    public DateTimeOffset LastOnline { get; set; }

    public bool IsBanned { get; set; }

    public string? Notes { get; set; }

    public string? Sso { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Avatar { get; set; }

    public int Patron { get; set; }

    public string? PatronBenefitUser { get; set; }

    public string? Permissions { get; set; }

    public bool Admin { get; set; }

    public int AlertsMax { get; set; }

    public int AlertsExpiry { get; set; }

    public bool AlertsUpdate { get; set; }

    public string? SsoDiscordId { get; set; }

    public string? SsoDiscordAvatar { get; set; }

    public int? SsoDiscordTokenExpires { get; set; }

    public string? SsoDiscordTokenAccess { get; set; }

    public string? SsoDiscordTokenRefresh { get; set; }

    public string? ApiPublicKey { get; set; }

    public string? ApiAnalyticsKey { get; set; }

    public int ApiRateLimit { get; set; }

    public static User FromReader(MySqlDataReader reader)
    {
        var notes = reader["notes"];
        var avatar = reader["avatar"];
        var patronBenefitUser = reader["patron_benefit_user"];
        var permissions = reader["permissions"];
        var ssoDiscordId = reader["sso_discord_id"];
        var ssoDiscordAvatar = reader["sso_discord_avatar"];
        var ssoDiscordTokenExpires = reader["sso_discord_token_expires"];
        var ssoDiscordTokenAccess = reader["sso_discord_token_access"];
        var ssoDiscordTokenRefresh = reader["sso_discord_token_refresh"];
        var apiPublicKey = reader["api_public_key"];
        var apiAnalyticsKey = reader["api_analytics_key"];
        return new User
        {
            Id = new UserId((Guid)reader["id"]),
            Added = DateTimeOffset.FromUnixTimeSeconds((int)reader["added"]),
            LastOnline = DateTimeOffset.FromUnixTimeSeconds((int)reader["last_online"]),
            IsBanned = (bool)reader["is_banned"],
            Notes = (string?)(notes == DBNull.Value ? null : notes),
            Sso = (string)reader["sso"],
            Username = (string)reader["username"],
            Email = (string)reader["email"],
            Avatar = (string?)(avatar == DBNull.Value ? null : avatar),
            Patron = (int)reader["patron"],
            PatronBenefitUser = (string?)(patronBenefitUser == DBNull.Value ? null : patronBenefitUser),
            Permissions = (string?)(permissions == DBNull.Value ? null : permissions),
            Admin = (bool)reader["admin"],
            AlertsMax = (int)reader["alerts_max"],
            AlertsExpiry = (int)reader["alerts_expiry"],
            AlertsUpdate = (bool)reader["alerts_update"],
            SsoDiscordId = (string?)(ssoDiscordId == DBNull.Value ? null : ssoDiscordId),
            SsoDiscordAvatar = (string?)(ssoDiscordAvatar == DBNull.Value ? null : ssoDiscordAvatar),
            SsoDiscordTokenExpires = (int?)(ssoDiscordTokenExpires == DBNull.Value ? null : ssoDiscordTokenExpires),
            SsoDiscordTokenAccess = (string?)(ssoDiscordTokenAccess == DBNull.Value ? null : ssoDiscordTokenAccess),
            SsoDiscordTokenRefresh = (string?)(ssoDiscordTokenRefresh == DBNull.Value ? null : ssoDiscordTokenRefresh),
            ApiPublicKey = (string?)(apiPublicKey == DBNull.Value ? null : apiPublicKey),
            ApiAnalyticsKey = (string?)(apiAnalyticsKey == DBNull.Value ? null : apiAnalyticsKey),
            ApiRateLimit = (int)reader["api_rate_limit"],
        };
    }
}