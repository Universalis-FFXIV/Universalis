using System.Text.Json.Serialization;

namespace Universalis.Application.Views.V2;

public class UserView
{
    /// <summary>
    /// The user's ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The user's creation time, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("created")]
    public long CreatedTimestampMs { get; set; }

    /// <summary>
    /// The user's last time online, in milliseconds since the UNIX epoch.
    /// </summary>
    [JsonPropertyName("lastOnline")]
    public long LastOnlineTimestampMs { get; set; }

    /// <summary>
    /// The user's username.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }

    /// <summary>
    /// The user's avatar.
    /// </summary>
    [JsonPropertyName("avatar")]
    public string Avatar { get; set; }

    /// <summary>
    /// The user's SSO information.
    /// </summary>
    [JsonPropertyName("sso")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SsoView Sso { get; set; }

    public class SsoView
    {
        /// <summary>
        /// The user's Discord SSO information.
        /// </summary>
        [JsonPropertyName("discord")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DiscordSsoView Discord { get; set; }

        public class DiscordSsoView
        {
            /// <summary>
            /// The user's Discord ID.
            /// </summary>
            [JsonPropertyName("id")]
            public string Id { get; set; }

            /// <summary>
            /// The user's Discord avatar.
            /// </summary>
            [JsonPropertyName("avatar")]
            public string Avatar { get; set; }
        }
    }
}